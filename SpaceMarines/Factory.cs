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

namespace SpaceMarines {
	/// <summary>
	/// //	https://stackoverflow.com/questions/24171730/adding-a-custom-type-name-to-all-classes-during-serialisation-with-json-net
	/// </summary>
	public class Factory : GameComponent {

		readonly JsonSerializerSettings settings;



		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public Factory( Game game ) : base(game)
		{
			settings = new JsonSerializerSettings();
			settings.Formatting = Newtonsoft.Json.Formatting.Indented;
			settings.Converters.Add( new StringEnumConverter { CamelCaseText = true } );
			settings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
			settings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple; 
			settings.TypeNameHandling = TypeNameHandling.All;
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
			using ( var reader = new StreamReader( stream ) ) {
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
			var text = JsonConvert.SerializeObject(obj, settings);

			using ( var writer = new StreamWriter( stream ) ) {
				writer.Write( text );
			}
		}

		
	}
}
