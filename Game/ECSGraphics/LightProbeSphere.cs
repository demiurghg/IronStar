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
	public class LightProbeSphere : IComponent, ITransformable
	{
		private Guid guid;

		[AECategory("Light probe")]
		[AEValueRange(0,256,8,0.25f)]
		public float Radius { get; set; } = 32;

		[AECategory("Light probe")]
		[AEDisplayName("Transition Width")]
		[AEValueRange(0.25f,64,1,0.25f)]
		public float Transition  { get; set; } = 8f;


		LightProbe light;
		LightSet lightSet;


		public LightProbeSphere ( Guid guid )
		{
			this.guid	=	guid;
		}


		private LightProbeSphere () : this( Guid.NewGuid() )
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
			light.ProbeMatrix		=	Matrix.Scaling( Radius ) * transform;

			light.Mode				=	LightProbeMode.SphereReflection;

			light.Radius			=	Radius;
			light.NormalizedWidth	=	Math.Max( 0, Radius * 2.0f - 2*Transition ) / (Radius * 2.0f) ;
			light.NormalizedHeight	=	Math.Max( 0, Radius * 2.0f - 2*Transition ) / (Radius * 2.0f) ;
			light.NormalizedDepth	=	Math.Max( 0, Radius * 2.0f - 2*Transition ) / (Radius * 2.0f) ;

			var bmin	=	transform.TranslationVector - Vector3.One * Radius; 
			var bmax	=	transform.TranslationVector + Vector3.One * Radius; 

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
