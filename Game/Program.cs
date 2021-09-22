using System;
using Fusion;
using Fusion.Build;
using Fusion.Development;
using Fusion.Core.Utils;
using Fusion.Build.Mapping;
using Fusion.Build.Processors;
using Fusion.Engine.Audio;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Engine.Graphics.GI;

namespace IronStar
{

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
			using (var game = new IronStar(builder)) {

				//	enable and disable debug direct3d device :
				game.RenderSystem.UseDebugDevice = false;
				game.RenderSystem.Fullscreen	= false;

				//	enable and disable object tracking :
				game.TrackObjects = false;

				//	apply command-line options here:
				//	...
				if (!LaunchBox.ShowDialog(game, "Config.ini", 
					()		=>	Log.Warning("Editor is in-game only"), 
					(cmd)	=>	game.Invoker.ExecuteString(cmd))
					) 
				{
					return 0;
				}

				//	run:
				game.Run();
			}

			return 0;
		}


		static Builder CreateBuilder()
		{
			var uiTextureProcessor      =   new TextureProcessor();
			var colorMapProcessor       =   new TextureProcessor();
			var normalMapProcessor      =   new TextureProcessor();
			var bc3TextureProcessor     =   new TextureProcessor(TextureCompression.BC3, true);
			var staticModelProcessor    =   new SceneProcessor( true,  true, 0, true, "" );
			var animationProcessor      =   new SceneProcessor( false, true, 0, true, "" );
			var weaponModelProcessor    =   new SceneProcessor( true,  true, 0, true, @"scenes\weapon2\weapon_common.fbx" );
			var jsonClassProcessor      =   new JsonProcessor();
			var textProcessor           =   new TextProcessor();
			var luaProcessor            =   new LuaProcessor();
			var fontProcessor           =   new FontProcessor();
			var textureAtlasProcessor   =   new TextureAtlasProcessor();
			var ubershaderProcessor		=	new UbershaderProcessor();

			var copyFmodBank            =   new CopyProcessor(typeof(SoundBank));

			var builder =   new Builder("Content")

				.InputDirectory(@"..\..\..\..\Content")
				.TempDirectory (@"..\..\..\..\Temp")

				.Generate( new UbershaderGenerator() )
				.Generate( new VTGenerator(@"..\..\..\..\Content") )

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
				.Ignore("_maya/*"       )
				.Ignore("_outsource/*"  )
				.Ignore("_kitbash/*"    )
				.Ignore("*.wav"         )
				.Ignore("*.ogg"         )
				.Ignore("*.auto.hlsl"         )

				.ToolsDirectory(@"..\..\..\..\Engine\SDKs")
				.ToolsDirectory(@"..\..\..\..\Engine\Native\FScene\bin\x64\Release")
				.ToolsDirectory(@"..\..\..\..\Engine\SDKs\KopiLua\Luac\bin\x64\Release")

				.InputDirectory(@"..\..\..\..\Engine\Shaders")
				.InputDirectory(@"..\..\..\..\Engine\Widgets")
				.InputDirectory(@"..\..\..\..\Engine\Content")

				.Process("*.hlsl"               , ubershaderProcessor )

				.Process("*.tga"                , uiTextureProcessor )
				.Process("*.png"                , uiTextureProcessor )
				.Process("*.jpg"                , uiTextureProcessor )

				.Process("sky/*.tga"            , bc3TextureProcessor )

				.Process("*.bmfc"               , fontProcessor )
				.Process("*.atlas"              , textureAtlasProcessor )

				.Process("*.fbx"                , staticModelProcessor )
				.Process("*anim_*.fbx"          , animationProcessor )
				.Process("scenes/weapon2/*.fbx" , weaponModelProcessor )

				.Process("*.lua"                , luaProcessor )

				.Process("*.json"               , jsonClassProcessor )

				.Copy<SoundBank>("*.bank")

				.Copy<LightMap>			("maps/lightmaps/*.bin")
				.Copy<LightProbeHDRI>	("maps/lightprobes/*.bin")
				;

			return builder;       
	    }
	}
}
