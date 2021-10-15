using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using Fusion.Engine.Graphics;
using RSLightProbe = Fusion.Engine.Graphics.LightProbe;
using Fusion.Core.Shell;
using Fusion.Core;

namespace IronStar.SFX2
{
	public class LightProbeSystem : ProcessingSystem<RSLightProbe,LightProbeSphere,LightProbeBox,Transform>
	{
		Dictionary<uint,RSLightProbe> lights = new Dictionary<uint, RSLightProbe>();

		readonly LightSet ls;

		
		public LightProbeSystem( RenderSystem rs )
		{
			this.ls	=	rs.RenderWorld.LightSet;
		}


		public override Aspect GetAspect()
		{
			return new Aspect()
				.Include<Transform>()
				.Single<LightProbeSphere,LightProbeBox>()
				;
		}


		protected override RSLightProbe Create( Entity e, LightProbeSphere lpSph, LightProbeBox lpBox, Transform t )
		{
			string name = "";
			if (lpSph!=null) name = lpSph.name;
			if (lpBox!=null) name = lpBox.name;

			var light = new RSLightProbe(name);

			Process( e, GameTime.Zero, light, lpSph, lpBox, t );

			ls.LightProbes.Add( light );

			return light;
		}


		protected override void Destroy( Entity e, RSLightProbe light )
		{
			ls.LightProbes.Remove( light );
		}

		
		protected override void Process( Entity e, GameTime gameTime, RSLightProbe light, LightProbeSphere lpSph, LightProbeBox lpBox, Transform t )
		{
			var transform	=	t.TransformMatrix;

			if (lpBox!=null)
			{
				var width	=	lpBox.Width;
				var height	=	lpBox.Height;
				var depth	=	lpBox.Depth;

				var shellWidth	=	lpBox.ShellWidth;
				var shellHeight	=	lpBox.ShellWidth;
				var shellDepth	=	lpBox.ShellWidth;

				light.ProbeMatrix		=	Matrix.Scaling( width/2.0f, height/2.0f, depth/2.0f ) * transform;

				light.NormalizedWidth	=	Math.Max( 0, width  - 2*shellWidth  ) / width	;
				light.NormalizedHeight	=	Math.Max( 0, height - 2*shellHeight ) / height;
				light.NormalizedDepth	=	Math.Max( 0, depth  - 2*shellDepth  ) / depth	;

				var size	=	MathUtil.Max3( width, height, depth ) * 0.5f;
				var bmin	=	transform.TranslationVector - Vector3.One * size; 
				var bmax	=	transform.TranslationVector + Vector3.One * size; 

				light.BoundingBox		=	new BoundingBox( bmin, bmax );
			}

			if (lpSph!=null)
			{
				var radius				=	lpSph.Radius;
				var transition			=	lpSph.Transition;

				light.ProbeMatrix		=	Matrix.Scaling( radius ) * transform;

				light.Mode				=	LightProbeMode.SphereReflection;

				light.Radius			=	radius;
				light.NormalizedWidth	=	Math.Max( 0, radius * 2.0f - 2*transition ) / (radius * 2.0f) ;
				light.NormalizedHeight	=	Math.Max( 0, radius * 2.0f - 2*transition ) / (radius * 2.0f) ;
				light.NormalizedDepth	=	Math.Max( 0, radius * 2.0f - 2*transition ) / (radius * 2.0f) ;

				var bmin	=	transform.TranslationVector - Vector3.One * radius; 
				var bmax	=	transform.TranslationVector + Vector3.One * radius; 

				light.BoundingBox		=	new BoundingBox( bmin, bmax );
			}
		}
	}
}
