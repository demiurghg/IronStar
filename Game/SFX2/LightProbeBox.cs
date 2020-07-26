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
	public class LightProbeBox : IComponent, ITransformable
	{
		private Guid guid;

		[AECategory("Light probe")]
		[AEValueRange(0,256,8,0.25f)]
		public float Width { get; set; } = 16;

		[AECategory("Light probe")]
		[AEValueRange(0,256,8,0.25f)]
		public float Height { get; set; } = 16;

		[AECategory("Light probe")]
		[AEValueRange(0,256,8,0.25f)]
		public float Depth  { get; set; } = 16;

		[AECategory("Light probe")]
		[AEDisplayName("Transition Width")]
		[AEValueRange(0.25f,32,1,0.25f)]
		public float ShellWidth  { get; set; } = 8f;

		[AECategory("Light probe")]
		[AEDisplayName("Transition Height")]
		[AEValueRange(0.25f,32,1,0.25f)]
		public float ShellHeight  { get; set; } = 8f;

		[AECategory("Light probe")]
		[AEDisplayName("Transition Depth")]
		[AEValueRange(0.25f,32,1,0.25f)]
		public float ShellDepth  { get; set; } = 8f;


		LightProbe light;
		LightSet lightSet;


		public LightProbeBox ( Guid guid )
		{
			this.guid	=	guid;
		}


		private LightProbeBox () : this( Guid.NewGuid() )
		{
		}


		public void Added( GameState gs, Entity entity )
		{
			lightSet	=	gs.GetService<RenderSystem>().RenderWorld.LightSet;
			light		=	new LightProbe( guid, 0 );	
			lightSet.LightProbes.Add( light );
		}


		public void Removed( GameState gs )
		{
			lightSet.LightProbes.Remove( light );
		}


		public void SetTransform( Matrix transform )
		{
			light.ProbeMatrix		=	Matrix.Scaling( Width/2.0f, Height/2.0f, Depth/2.0f ) * transform;

			light.NormalizedWidth	=	Math.Max( 0, Width  - 2*ShellWidth  ) / Width	;
			light.NormalizedHeight	=	Math.Max( 0, Height - 2*ShellHeight ) / Height;
			light.NormalizedDepth	=	Math.Max( 0, Depth  - 2*ShellDepth  ) / Depth	;

			var size	=	MathUtil.Max3( Width, Height, Depth ) * 0.5f;
			var bmin	=	transform.TranslationVector - Vector3.One * size; 
			var bmax	=	transform.TranslationVector + Vector3.One * size; 

			light.BoundingBox		=	new BoundingBox( bmin, bmax );
		}


		public void Save( GameState gs, Stream stream )
		{
			throw new NotImplementedException();
		}

		public void Load( GameState gs, Stream stream )
		{
			throw new NotImplementedException();
		}
	}
}
