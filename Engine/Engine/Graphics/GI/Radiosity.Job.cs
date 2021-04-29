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

namespace Fusion.Engine.Graphics.GI
{
	public partial class Radiosity : RenderComponent
	{
		public class Job
		{
			enum Stage
			{
				BvhBuilding,
				Rasterizing,
				Illuminating,
				Tracing,
				Filtering,
				Saving,
			}

			readonly Radiosity rad;
			readonly Rectangle LightmapRegion;
			readonly int TotalBounces;

			int bounceCounter = 0;
			int regionCounter = 0;

			public Job( Radiosity rad, int totalBounces, Rectangle lightMapRegion, int regionSize )
			{
				this.rad		=	rad;
				LightmapRegion	=	lightMapRegion;
				TotalBounces	=	totalBounces;
			}


			public bool Advance()
			{
				
				return true;
			}
		}
	}
}
