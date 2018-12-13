using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Mathematics;
using Fusion.Core.Shell;
using Fusion.Engine.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Fusion.Core.IniParser;
using Fusion.Core;

namespace Fusion.Build.Processors {


	public class TextureAtlas {
		
		public int Width = 512;
		public int Height = 512;
		public bool Compression = false;
		public int Padding = 0;
		public bool Mips = false;
		public Color FillColor = Color.Zero;
		public readonly List<TextureAtlasAnimation> Animations;


		public TextureAtlas ()
		{
			Animations = new List<TextureAtlasAnimation>();
		}



		public TextureAtlasFrame[] GetAllFrames ()
		{
			var list = new List<TextureAtlasFrame>();

			foreach ( var anim in Animations ) {
				foreach ( var frame in anim.Frames ) {	
					list.Add( frame );
				}
			}
			return list.ToArray();
		}


		public void LoadTextures ( string basePath )
		{
			foreach ( var anim in Animations ) {
				foreach ( var frame in anim.Frames ) {	
					frame.LoadImage( basePath );
				}
			}
		}



		public static TextureAtlas ReadFromIniFile ( string path )
		{
			var ip = new StreamIniDataParser();
			ip.Parser.Configuration.AllowDuplicateSections	=	false;
			ip.Parser.Configuration.AllowDuplicateKeys		=	false;
			ip.Parser.Configuration.CommentString			=	"#";
			ip.Parser.Configuration.OverrideDuplicateKeys	=	false;
			ip.Parser.Configuration.KeyValueAssigmentChar	=	'=';

			using ( var reader = new StreamReader( File.OpenRead( path ) ) ) {

				var iniData = ip.ReadData( reader );

				var sectionTextures		= iniData.Sections["Textures"];
				var sectionGeneral		= iniData.Global;

				if (sectionGeneral==null) {
					throw new InvalidDataException("Missing [General] section");
				}
				
				if (sectionTextures==null) {
					throw new InvalidDataException("Missing [Textures] section");
				}

				var atlas = new TextureAtlas();
				var split = (" \t,;").ToArray();

				atlas.Width			=	StringConverter.ToInt32	 ( sectionGeneral["Width"		] ?? "512"		);
				atlas.Height		=	StringConverter.ToInt32	 ( sectionGeneral["Height"		] ?? "512"		);
				atlas.Padding		=	StringConverter.ToInt32	 ( sectionGeneral["Padding"		] ?? "0"		);
				atlas.Compression	=	StringConverter.ToBoolean( sectionGeneral["Compression"	] ?? "false"	);
				atlas.Mips			=	StringConverter.ToBoolean( sectionGeneral["Mips		"	] ?? "false"	);
				atlas.FillColor		=	StringConverter.ToColor	 ( sectionGeneral["FillColor"	] ?? "0 0 0 0"	);

				foreach ( var keyValue in sectionTextures ) {
					var name		=	keyValue.KeyName;
					var textures	=	keyValue.Value.Split(split, StringSplitOptions.RemoveEmptyEntries);
					var animation	=	new TextureAtlasAnimation( name, textures );
					atlas.Animations.Add( animation );
				}

				return atlas;
			}
		}



		public static void WriteToFile ( string path, TextureAtlas atlas )
		{
			var settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			settings.Converters.Add( new StringEnumConverter { CamelCaseText = true } );
			settings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple; 

			JsonConvert.SerializeObject( atlas, settings );
		}
	}

}