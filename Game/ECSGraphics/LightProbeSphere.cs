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

namespace IronStar.SFX2
{
	public class LightProbeSphere : IComponent
	{
		public readonly Guid guid;

		[AECategory("Light probe")]
		[AEValueRange(0,256,8,0.25f)]
		public float Radius { get; set; } = 32;

		[AECategory("Light probe")]
		[AEDisplayName("Transition Width")]
		[AEValueRange(0.25f,64,1,0.25f)]
		public float Transition  { get; set; } = 8f;

		public LightProbeSphere ( Guid guid )
		{
			this.guid	=	guid;
		}


		private LightProbeSphere () : this( Guid.NewGuid() )
		{
		}

		public void Save( GameState gs, Stream stream ) {}
		public void Load( GameState gs, Stream stream ) {}
		public void Added( GameState gs, Entity entity ) {}
		public void Removed( GameState gs ) {}
	}
}
