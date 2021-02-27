﻿using System;
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
using Fusion.Engine.Graphics.Bvh;
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

		class BakeLightMapCmd : ICommand
		{
			readonly Radiosity	rad;

			[CommandLineParser.Name("bounce")]
			[CommandLineParser.Option]
			public int NumBounces 
			{ 
				get { return numBounces; }
				set 
				{
					numBounces = MathUtil.Clamp( value, 1, 3 );
				}
			}
			int numBounces = 1;

			[CommandLineParser.Name("rays")]
			[CommandLineParser.Option]
			public int NumRays 
			{ 
				get { return numRays; }
				set 
				{
					numRays = MathUtil.Clamp( value, 16, 1024 );
				}
			}
			int numRays = 256;

			[CommandLineParser.Name("nofilter")]
			[CommandLineParser.Option]
			public bool NoFilter { get; set; }

			[CommandLineParser.Name("white")]
			[CommandLineParser.Option]
			public bool White { get; set; }

			[CommandLineParser.Name("mapname")]
			[CommandLineParser.Required]
			public string MapName { get; set; }


			public BakeLightMapCmd( Radiosity rad )
			{
				this.rad	=	rad;
			}

			public object Execute()
			{
				using ( var stream = rad.Game.GetService<Builder>().CreateSourceFile( RenderSystem.LightmapPath, MapName + ".bin" ) )
				{
					var settings = new RadiositySettings();

					settings.NumRays		=	NumRays;
					settings.NumBounces		=	NumBounces;
					settings.UseFilter		=	!NoFilter;
					settings.WhiteDiffuse	=	White;

					rad.BakeRadiosity( settings, stream );
				}

				return null;
			}
		}
	}
}
