using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using Fusion.Engine.Graphics;
using RSOmniLight = Fusion.Engine.Graphics.OmniLight;
using Fusion.Core.Shell;
using System.IO;
using Fusion.Widgets.Advanced;

namespace IronStar.SFX2
{
	public class LightProbeSphere : Component
	{
		public readonly string name;

		[AECategory("Light probe")]
		[AESlider(0,256,8,0.25f)]
		public float Radius { get; set; } = 32;

		[AECategory("Light probe")]
		[AEDisplayName("Transition Width")]
		[AESlider(0.25f,64,1,0.25f)]
		public float Transition  { get; set; } = 8f;

		public LightProbeSphere ( string name )
		{
			this.name	=	name;
		}


		private LightProbeSphere () : this( Guid.NewGuid().ToString() )
		{
		}
	}
}
