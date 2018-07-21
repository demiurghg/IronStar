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


	public class Animation {
		public AnimState	State;
		public string		Take;
		public string		NextAnim;
		public int			NextKey;
		public Transition[] Transitions;
		public EffectTag[]	Effects;
		public string		Name { get { return Take; } }
	}

	public class Transition {
		public AnimState	State;
		public int			Low;
		public int			High;
		public string		NextAnim;
		public int			NextKey;
	}

	public class EffectTag {
		public string	Sfx;
		public int		Key;
		public string	Joint;
	}


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

		public List<Animation> Animations;

		public AnimatorFactory ()
		{
			Animations = new List<Animation>();
		}

		public Animator Create ( Scene scene )
		{
			throw new NotImplementedException();
		}
	}
}
