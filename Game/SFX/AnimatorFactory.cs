using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Extensions;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Fusion.Engine.Graphics;
using IronStar.Core;

namespace IronStar.SFX {

	#region AnimController Loader
	[ContentLoader(typeof(AnimatorFactory))]
	public class AnimControllerLoader : ContentLoader {
		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			var settings = new JsonSerializerSettings();
			settings.Formatting = Newtonsoft.Json.Formatting.Indented;
			settings.Converters.Add( new StringEnumConverter { CamelCaseText = true } );
			settings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
			settings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple; 
			settings.TypeNameHandling = TypeNameHandling.All;

			using ( var reader = new StreamReader( stream ) ) {
				var text = reader.ReadToEnd();
				return JsonConvert.DeserializeObject(text, typeof(AnimatorFactory), settings);
			}
		}
	}
	#endregion


	public class AnimatorFactory {

		public AnimatorFactory ()
		{
		}

		public Animator Create ( Scene scene )
		{
			throw new NotImplementedException();
		}
	}
}
