using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Engine.Common;
using System.Xml.Serialization;
using System.IO;
using Fusion.Core.Extensions;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Runtime.CompilerServices;

namespace Fusion.Core {
	/// <summary>
	/// //	https://stackoverflow.com/questions/24171730/adding-a-custom-type-name-to-all-classes-during-serialisation-with-json-net
	/// </summary>
	public class JsonFactory : GameComponent {

		public static JsonSerializerSettings CreateSettings( bool idented )
		{
			var settings = new JsonSerializerSettings();

			settings.Formatting = Newtonsoft.Json.Formatting.Indented;
			settings.Converters.Add( new StringEnumConverter { CamelCaseText = true } );
			settings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
			settings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple; 
			settings.TypeNameHandling = TypeNameHandling.All;

			return settings;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public JsonFactory( Game game ) : base(game)
		{
		}


		/// <summary>
		/// 
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		public object ImportJson ( Stream stream )
		{
			var settings = CreateSettings(true);

			using ( var reader = new StreamReader( stream ) ) 
			{
				var text = reader.ReadToEnd();
				return JsonConvert.DeserializeObject(text, settings);
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="obj"></param>
		public void ExportJson ( Stream stream, object obj )
		{
			var settings = CreateSettings(true);

			var text = JsonConvert.SerializeObject(obj, settings);

			using ( var writer = new StreamWriter( stream ) ) {
				writer.Write( text );
			}
		}


		public string ExportJsonString( object obj )
		{
			var settings = CreateSettings(true);
			return JsonConvert.SerializeObject(obj, settings);
		}

		
		public object ImportJsonString( string text )
		{
			var settings = CreateSettings(true);
			return JsonConvert.DeserializeObject(text, settings);
		}

		
	}
}
