using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.IniParser;
using Fusion.Core.IniParser.Model;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Core;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Fusion.Core.Configuration 
{
	public class ConfigManager 
	{
		IniData settings;
		readonly Game game;

		object lockObject = new object();


		public ConfigManager ( Game game )
		{
			this.game	=	game;
			settings	=	new IniData();

			foreach ( var cfg in GetConfigClasses() )
			{
				RuntimeHelpers.RunClassConstructor( cfg.TypeHandle );
			}
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Private core functions :
		-----------------------------------------------------------------------------------------------*/

		private void ApplySettings ( string sectionName, Type targetType, object targetObject, bool importAll )
		{
			lock (lockObject) 
			{
				var section = settings.Sections[sectionName];

				if (section==null) 
				{
					Log.Warning("Section [{0}] does not exist.", sectionName);
					return;
				}

				var props = targetType
							.GetProperties(BindingFlags.Public|BindingFlags.Static)
							.Where( p1 => p1.HasAttribute<ConfigAttribute>() || importAll )
							.ToArray();
			
				foreach ( var prop in props ) 
				{
					var name	=	prop.Name;
					var type	=	prop.PropertyType;
					var keyData =	section.GetKeyData( name );

					if (keyData==null) 
					{	
						Log.Warning("Key '{0}' does not exist in section [{1}].", name, sectionName );
						continue;
					}

					object value;

					if ( StringConverter.TryConvertFromString( type, keyData.Value, out value ) ) 
					{
						prop.SetValue( targetObject, value );	
					} 
					else 
					{
						Log.Warning("Can not convert key '{0}' to {1}.", name, type.Name );
					}
				}
			}
		}


		private void RetrieveSettings ( string sectionName, Type sourceType, object sourceObject, bool exportAll )
		{
			lock (lockObject) 
			{
				var sectionData = new SectionData( sectionName );

				if (sectionData==null) 
				{
					Log.Warning("Section [{0}] does not exist.", sectionName);
					return;
				}

				var props = sourceType
							.GetProperties(BindingFlags.Public|BindingFlags.Static)
							.Where( p1 => p1.HasAttribute<ConfigAttribute>() || exportAll )
							.ToArray();
			
				foreach ( var prop in props ) 
				{
					var name	=	prop.Name;
					var type	=	prop.PropertyType;
					var value	=	StringConverter.ConvertToString( prop.GetValue(sourceObject) );

					sectionData.Keys.AddKey( name, value );
				}

				settings.Sections.RemoveSection(sectionName);
				settings.Sections.SetSectionData( sectionName, sectionData );
			}
		}


		void ApplyStaticSettings ()
		{
			Misc.GetAllClassesWithAttribute<ConfigClassAttribute>()
				.Select( type2 => new { Name = type2.GetCustomAttribute<ConfigClassAttribute>().NiceName ?? type2.Name, Type = type2 } )
				.ToList()
				.ForEach( nameType => ApplySettings( nameType.Name, nameType.Type, null, false ) );
		}


		void RetrieveStaticSettings ()
		{
			Misc.GetAllClassesWithAttribute<ConfigClassAttribute>()
				.Select( type2 => new { Name = type2.GetCustomAttribute<ConfigClassAttribute>().NiceName ?? type2.Name, Type = type2 } )
				.ToList()
				.ForEach( nameType => RetrieveSettings( nameType.Name, nameType.Type, null, false ) );
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Public export/import function
		-----------------------------------------------------------------------------------------------*/

		public static Type[] GetConfigClasses()
		{
			return Misc.GetAllClassesWithAttribute<ConfigClassAttribute>();
		}

		/// <summary>
		/// Loads file from specified file
		/// </summary>
		/// <param name="filename"></param>
		public void LoadSettings ( string filename )
		{
			Log.Message("Loading configuration...");

			var storage = game.UserStorage;

			if (storage.FileExists(filename)) 
			{
				LoadSettings( storage.OpenFile(filename, FileMode.Open, FileAccess.Read) );
			} 
			else 
			{
				Log.Warning("Can not load configuration from {0}", filename);
			}
		}


		/// <summary>
		/// Saves settings to specified file
		/// </summary>
		/// <param name="filename"></param>
		public void SaveSettings ( string filename )
		{
			Log.Message("Saving configuration...");

			var storage = game.UserStorage;

			storage.DeleteFile(filename);
			SaveSettings( storage.OpenFile(filename, FileMode.Create, FileAccess.Write) );
		}


		/// <summary>
		/// Loads settings from stream
		/// </summary>
		/// <param name="stream"></param>
		public void LoadSettings ( Stream stream )
		{
			lock (lockObject) 
			{
				var iniData = new IniData();
				var parser = new StreamIniDataParser();

				parser.Parser.Configuration.CommentString	=	"# ";

				using ( var sw = new StreamReader(stream) ) 
				{
					settings	= parser.ReadData( sw );
				}

				ApplyStaticSettings();
			}
		}


		/// <summary>
		/// Saves settings to stream
		/// </summary>
		/// <param name="stream"></param>
		public void SaveSettings ( Stream stream )
		{
			lock (lockObject) 
			{
				RetrieveStaticSettings();

				settings.Configuration.CommentString	=	"# ";

				var parser = new StreamIniDataParser();

				using ( var sw = new StreamWriter(stream) ) {
					parser.WriteData( sw, settings );
				}
			}
		}
	}
}
