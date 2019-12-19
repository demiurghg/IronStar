using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Fusion;
using Fusion.Build;
using Fusion.Development;
using Fusion.Engine.Common;
using Fusion.Core;
using Fusion.Core.Shell;
using Fusion.Core.Utils;
using Fusion.Engine.Imaging;
using IronStar.Core;
using IronStar.Mapping;
using Fusion.Core.Extensions;
using Fusion.Build.Mapping;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using IronStar.Editor;
using Fusion.Core.Mathematics;
using Fusion.Scripting;
using Fusion.Build.Processors;
using Fusion.Engine.Audio;

namespace IronStar {

	class Program {

		[STAThread]
		static int Main ( string[] args )
		{
			// 	colored console output :
			Log.AddListener( new ColoredLogListener() );

			//	output for in-game console :
			Log.AddListener( new LogRecorder() );

			//	set verbosity :
			Log.VerbosityLevel = LogMessageType.Verbose;

			//
			//	Build content on startup.
			//	Remove this line in release code.
			//
			var builder = CreateBuilder();
			builder.Build();

			//
			//	Run game :
			//
			using (var game = new IronStar()) {

				//	enable and disable debug direct3d device :
				game.RenderSystem.UseDebugDevice = false;
				game.RenderSystem.Fullscreen	= false;

				//	enable and disable object tracking :
				game.TrackObjects = false;

				//	apply command-line options here:
				//	...
				if (!LaunchBox.ShowDialog(game, "Config.ini", ()=>Log.Warning("Editor is in-game only"))) {
					return 0;
				}

				//	run:
				game.Run();
			}

			return 0;
		}


		static Builder2 CreateBuilder()
		{
			var uiTextureProcessor      =   new TextureProcessor();
			var colorMapProcessor       =   new TextureProcessor();
			var normalMapProcessor      =   new TextureProcessor();
			var staticModelProcessor    =   new SceneProcessor( true, false, 0, true, "" );
			var animationProcessor      =   new SceneProcessor( false, true, 0, true, "" );
			var weaponModelProcessor    =   new SceneProcessor( true,  true, 0, true, @"scenes\weapon\baseAnimation.fbx" );
			var jsonClassProcessor      =   new JsonProcessor();
			var textProcessor           =   new TextProcessor();
			var luaProcessor            =   new LuaProcessor();
			var fontProcessor           =   new FontProcessor();
			var textureAtlasProcessor   =   new TextureAtlasProcessor();
       
			var copyFmodBank            =   new CopyProcessor(typeof(SoundBank));
       
			var builder =   new Builder2("Content")
       
				.InputDirectory(@"..\..\..\..\Content")
				.TempDirectory (@"..\..\..\..\Temp")
       
				.Ignore("*.ma"          )
				.Ignore("*.psd"         )
				.Ignore("*_highPoly.fbx")
				.Ignore("*_lowPoly.fbx" )
				.Ignore("*_highpoly.fbx")
				.Ignore("*_lowpoly.fbx" )
				.Ignore("*_high.fbx"    )
				.Ignore("spots/*.tga"   )
				.Ignore("sprites/*.tga" )
				.Ignore("decals/*.tga"  )
				.Ignore("scenes/*.tga"  )
				.Ignore("scenes/*.png"  )
				.Ignore("scenes/*.jpg"  )
				.Ignore("turtle/*"      )
				.Ignore("_outsource/*"  )
				.Ignore("_kitbash/*"    )
				.Ignore("*.wav"         )
				.Ignore("*.ogg"         )
           
				.ToolsDirectory(@"..\..\..\..\Engine\SDKs")
				.ToolsDirectory(@"..\..\..\..\Engine\Native\FScene\bin\x64\Release")
				.ToolsDirectory(@"..\..\..\..\Engine\SDKs\KopiLua\Luac\bin\x64\Release")

				.InputDirectory(@"..\..\..\..\Engine\Shaders")
           
				.Process("*.tga"                , uiTextureProcessor )
				.Process("*.png"                , uiTextureProcessor )
				.Process("*.jpg"                , uiTextureProcessor )
           
				.Process("*.atlas"              , textureAtlasProcessor )
           
				.Process("*.fbx"                , staticModelProcessor )
				.Process("*anim_*.fbx"          , animationProcessor )
				.Process("scenes/weapon2/*.fbx" , staticModelProcessor )
           
				.Process("*.lua"                , luaProcessor )
           
				.Process("*.json"               , jsonClassProcessor )
           
				.Copy<SoundBank>("*.bank")
           
				//.Copy<IrradianceMap>("*.irrmap"  )
				//.Copy<IrradianceMap>("*.irrvol"  )
				//.Copy<IrradianceMap>("*.irrcache")
				;

			return builder;       
	    }
	}
}
