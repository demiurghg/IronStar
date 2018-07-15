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
using Newtonsoft.Json.Converters;
using Fusion.Engine.Graphics;
using IronStar.Core;

namespace IronStar.SFX {


	public class AnimTake {
		public AnimState	State;
		public string		TakePath;
		public int			Length;
		public float		Rate;
		public string		NextTake;
		public int			NextKey;
		public AnimTransition[] Transitions;
		public AnimEffect[]		Effects;

		Scene take;

		public void LoadTake ( ContentManager content, string baseDir )
		{
			take	=	content.Load<Scene>( Path.Combine( baseDir, TakePath ) );
			//Length	=	take.LastFrame
		}
	}

	public class AnimTransition {
		public AnimState	State;
		public int			Low;
		public int			High;
		public string		NextTake;
		public int			NextKey;
	}

	public class AnimEffect {
		public string	Sfx;
		public int		Key;
		public string	Joint;
	}


	#region AnimController Loader
	[ContentLoader(typeof(AnimController))]
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
				return JsonConvert.DeserializeObject(text, typeof(AnimController), settings);
			}
		}
	}
	#endregion


	public class AnimController {

		public Dictionary<string,AnimTake> Takes;

		public AnimController ()
		{
			Takes = new Dictionary<string, AnimTake>();
		}


		public void Update ()
		{
		}
	}
}
