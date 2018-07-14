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

namespace IronStar.SFX {

	public enum AnimState : byte {
		Weapon_Idle,
		Weapon_Warmup,
		Weapon_Attack,
		Weapon_Cooldown,
		Weapon_Reload,
		Weapon_Overheat,
		Weapon_Drop,
		Weapon_Raise,
		Weapon_NoAmmo,
	}


	public class AnimTake {
		public AnimState	State;
		public string		TakePath;
		public int			Length;
		public float		Rate;
		public string		NextTake;
		public int			NextKey;
		public AnimTransition[] Transitions;
		public AnimEffect[]		Effects;
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

		public void LoadTakes ( ContentManager content, string baseScenePath )
		{
		}

		public void Update ()
		{
		}
	}
}
