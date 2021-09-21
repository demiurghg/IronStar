using System;
using Fusion;
using Fusion.Build;
using Fusion.Build.Processors;
using Fusion.Core.Utils;

namespace SpriteDemo
{
	class Program
	{
		static void Main( string[] args )
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
			using (var game = new SpriteDemo("SpriteDemo", "SpriteDemo")) 
			{
				game.RenderSystem.UseDebugDevice	=	false;	//	do not use debug graphics device
				game.RenderSystem.Fullscreen		=	false;	//	no not use fullscreen mode on startup
				game.RenderSystem.UseRenderDoc		=	false;	//	do not use debug graphics device

				game.TrackObjects					=	false;	//	do not track objects

				//	run game
				game.Run();
			}
		}

		static Builder CreateBuilder()
		{
			//	Create content processors.
			//	For this sample we need only texture and shader processors:
			var textureProcessor		=	new TextureProcessor();
			var ubershaderProcessor		=	new UbershaderProcessor();

			//	create content builder with specified output directory
			var builder =   new Builder("Content")

				//	provide content and temporary directories :
				.InputDirectory(@"..\..\..\Content")
				.TempDirectory (@"..\..\..\Temp")
				
				//	set file masks to ignore :
				.Ignore("*.auto.hlsl" )

				//	provide directories for command line tools :
				.ToolsDirectory(@"..\..\..\..\..\Engine\SDKs")
				.ToolsDirectory(@"..\..\..\..\..\Engine\Native\FScene\bin\x64\Release")
				.ToolsDirectory(@"..\..\..\..\..\Engine\SDKs\KopiLua\Luac\bin\x64\Release")

				//	provide additional content directories :
				.InputDirectory(@"..\..\..\..\..\Engine\Shaders")

				//	associate file extensions with processors :
				.Process("*.hlsl"		, ubershaderProcessor )
				.Process("*.tga"		, textureProcessor )
				.Process("*.png"		, textureProcessor )
				.Process("*.jpg"		, textureProcessor )
				;

			return builder;
		}
	}
}
