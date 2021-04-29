using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Content;
using Fusion.Core.Configuration;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Core;
using Fusion.Engine.Graphics.Lights;
using Fusion.Core.Shell;
using Fusion.Engine.Graphics.Collections;
using System.Diagnostics;
using Fusion.Build;

namespace Fusion.Engine.Graphics.GI
{
	public partial class Radiosity : RenderComponent
	{
		public class BakeRadiosityCommand : ICommand
		{
			readonly RadiositySettings settings;
			readonly string mapName;
			readonly Game game;

			public BakeRadiosityCommand( Game game, string mapName, RadiositySettings settings )
			{
				this.game		=	game;
				this.mapName	=	mapName;
				this.settings	=	settings;
			}

			public object Execute()
			{
				var rad = game.RenderSystem.Radiosity;
				var build = game.GetService<Builder>();
				
				using ( var stream = build.CreateSourceFile( RenderSystem.LightmapPath, mapName + ".bin" ) )
				{
					rad.BakeRadiosity( settings, stream );
				}

				return null;
			}
		}
	}
}
