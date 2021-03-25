using Fusion.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Engine.Audio;
using Fusion.Engine.Frames;
using Fusion.Engine.Tools;
using Fusion;
using Fusion.Core.Shell;
using IronStar.Editor;
using Fusion.Build;
using IronStar.SinglePlayer;

namespace IronStar {
	partial class IronStar : Game
	{
		class MapCommand : ICommand
		{
			readonly IronStar game;

			[CommandLineParser.Name("mapname")]
			[CommandLineParser.Required]
			public string MapName { get; set; }

			[CommandLineParser.Name("edit")]
			[CommandLineParser.Option]
			public bool Edit { get; set; } = false;

			[CommandLineParser.Name("dedicated")]
			[CommandLineParser.Option]
			public bool Dedicated { get; set; } = false;

			public MapCommand ( IronStar game )
			{
				this.game	=	game;
			}

			public object Execute()
			{
				if (Edit) 
				{
					game.StartEditor(MapName);
				} 
				else 
				{
					game.GetService<Mission>().State.Start(MapName);
				}
				return null;
			}

		}



		class KillServerCommand : ICommand 
		{

			readonly IronStar game;

			public KillServerCommand ( IronStar game )
			{
				this.game = game;
			}

			public object Execute()
			{
				game.KillServer();
				return null;
			}
		}



		class ConnectCommand : ICommand
		{
			readonly IronStar game;

			[CommandLineParser.Required]
			[CommandLineParser.Name("Host")]
			public string Host { get; set; }

			[CommandLineParser.Required]
			[CommandLineParser.Name("Port")]
			public int Port { get; set; }

			public ConnectCommand ( IronStar game )
			{
				this.game = game;
			}

			public object Execute()
			{
				game.Connect( Host, Port );
				return null;
			}
		}



		class DisconnectCommand : ICommand {

			readonly IronStar game;

			public DisconnectCommand ( IronStar game )
			{
				this.game = game;
			}

			public object Execute()
			{
				game.Disconnect( "disconnect by user request" );
				return null;
			}
		}



		class KillEditorCommand : ICommand {

			readonly IronStar game;

			public KillEditorCommand ( IronStar game )
			{
				this.game = game;
			}

			public object Execute()
			{
				game.StopEditor();
				return null;
			}
		}



		class ContentBuildCommand : ICommand 
		{
			Game	 game;
			Builder builder;
			
			[CommandLineParser.Option]
			[CommandLineParser.Name("force")]
			public bool	Force { get; set; }

			[CommandLineParser.Option]
			[CommandLineParser.Name("clean")]
			public string Clean { get; set; }

			public ContentBuildCommand ( Game game, Builder builder )
			{
				this.game		=	game;
				this.builder	=	builder;
			}

			public object Execute ()
			{
				if (Force) 
				{
					builder.RebuildAll();
				}
				else if (!string.IsNullOrWhiteSpace(Clean)) 
				{
					builder.Rebuild(Clean);
				}
				else 
				{
					builder.Build();
				}
				game.Reload();	

				return null;		
			}
		}


		class ContentFileCommand : ICommand 
		{
			public object Execute()
			{
				throw new NotImplementedException();
				//>>>>>>>>return Builder.Options.ContentIniFile;
			}
		}



		class ContentReportCommand : ICommand {

			[CommandLineParser.Option]
			[CommandLineParser.Name("reportFile")]
			public string ReportFile { get; set; }


			public ContentReportCommand()
			{
			}

			public object Execute() 
			{
				throw new NotImplementedException();
				//>>>>>>>>Builder.OpenReport( ReportFile );
			}

		}
	}
}
