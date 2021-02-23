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
using Fusion.Engine.Graphics.Bvh;
using System.Diagnostics;

namespace Fusion.Engine.Graphics.GI
{
	public partial class Radiosity : RenderComponent
	{
		class BakeRadiosityCmd : ICommand
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

			[CommandLineParser.Name("path")]
			[CommandLineParser.Required]
			public string OutputPath { get; set; }



			public BakeRadiosityCmd( Radiosity rad )
			{
				this.rad	=	rad;
			}

			public object Execute()
			{
				rad.BakeRadiosity( NumBounces, NumRays, !NoFilter, OutputPath );
				return null;
			}
		}
	}
}
