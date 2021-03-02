using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using Fusion.Engine.Graphics;
using Fusion.Core.Shell;
using Fusion.Widgets.Advanced;

namespace IronStar.SFX2
{
	public class LightVolume : Component
	{
		[AECategory("Light Volume")]
		[AESlider(4, 256, 4, 1)]
		public int ResolutionX 
		{ 
			get; set; 
		} = 16;
		
		[AECategory("Light Volume")]
		[AESlider(4, 256, 4, 1)]
		public int ResolutionY 
		{ 
			get; set; 
		} = 16;
		
		[AECategory("Light Volume")]
		[AESlider(4, 256, 4, 1)]
		public int ResolutionZ 
		{ 
			get; set; 
		} = 16;

		[AECategory("Light Volume")]
		[AESlider(4, 1024, 32, 4)]
		public float Width 
		{ 
			get; set; 
		}
		
		[AECategory("Light Volume")]
		[AESlider(4, 1024, 32, 4)]
		public float Height 
		{ 
			get; set; 
		}
		
		[AECategory("Light Volume")]
		[AESlider(4, 1024, 32, 4)]
		public float Depth 
		{ 
			get; set; 
		}
	}
}
