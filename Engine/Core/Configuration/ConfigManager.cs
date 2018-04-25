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
using Fusion.Engine.Input;
using Fusion.Engine.Storage;

namespace Fusion.Core.Configuration {
	public class ConfigManager {

		IniData settings;
		readonly Game game;


		public ConfigManager ( Game game )
		{
			this.game	=	game;
			settings	=	new IniData();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		public void ApplySettings ( object target )
		{
			string sectionName = target.GetType().Name;

			var section = settings.Sections[sectionName];

			if (section==null) {
				Log.Warning("Section [{0}] does not exist.", sectionName);
				return;
			}

			var props = target.GetType()
						.GetProperties()
						.Where( p1 => p1.HasAttribute<ConfigAttribute>() )
						.ToArray();
			
			foreach ( var prop in props ) {
			
				var name	=	prop.Name;
				var type	=	prop.PropertyType;
				var keyData =	section.GetKeyData( name );

				if (keyData==null) {	
					Log.Warning("Key '{0}' does not exist in section [{1}].", name, sectionName );
				}

				object value;

				if ( StringConverter.TryConvertFromString( type, keyData.Value, out value ) ) {
					prop.SetValue( target, value );	
				} else {
					Log.Warning("Can not convert key '{0}' to {1}.", name, type.Name );
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		public void RetrieveSettings ( object source )
		{
			string sectionName = source.GetType().Name;

			var sectionData = new SectionData( sectionName );

			if (sectionData==null) {
				Log.Warning("Section [{0}] does not exist.", sectionName);
				return;
			}

			var props = source.GetType()
						.GetProperties()
						.Where( p1 => p1.HasAttribute<ConfigAttribute>() )
						.ToArray();
			
			foreach ( var prop in props ) {
			
				var name	=	prop.Name;
				var type	=	prop.PropertyType;
				var value	=	StringConverter.ConvertToString( prop.GetValue(source) );

				sectionData.Keys.AddKey( name, value );
			}

			settings.Sections.RemoveSection(sectionName);
			settings.Sections.SetSectionData( sectionName, sectionData );
		}


		/// <summary>
		/// Loads file from specified file
		/// </summary>
		/// <param name="filename"></param>
		public void LoadSettings ( string filename )
		{
			Log.Message("Loading configuration...");

			var storage = game.UserStorage;

			if (storage.FileExists(filename)) {
				
				LoadSettings( storage.OpenFile(filename, FileMode.Open, FileAccess.Read) );

			} else {
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
			var iniData = new IniData();
			var parser = new StreamIniDataParser();

			parser.Parser.Configuration.CommentString	=	"# ";

			using ( var sw = new StreamReader(stream) ) {
				settings	= parser.ReadData( sw );
			}
		}


		/// <summary>
		/// Saves settings to stream
		/// </summary>
		/// <param name="stream"></param>
		public void SaveSettings ( Stream stream )
		{
			settings.Configuration.CommentString	=	"# ";

			var parser = new StreamIniDataParser();

			using ( var sw = new StreamWriter(stream) ) {
				parser.WriteData( sw, settings );
			}
		}

	}
}
