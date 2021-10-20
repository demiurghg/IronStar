using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using Fusion.Engine.Graphics;
using RSSpotLight = Fusion.Engine.Graphics.SpotLight;
using Fusion.Core.Shell;
using Fusion.Core;
using BEPUutilities.Threading;

namespace IronStar.SFX2
{
	public class SpotLightSystem : ProcessingSystem<RSSpotLight,SpotLight,Transform>
	{
		Dictionary<uint,RSSpotLight> lights = new Dictionary<uint, RSSpotLight>();

		readonly LightSet ls;

		
		public SpotLightSystem( RenderSystem rs )
		{
			this.ls	=	rs.RenderWorld.LightSet;
		}

		
		protected override RSSpotLight Create( Entity e, SpotLight ol, Transform t )
		{
			var light = new RSSpotLight();

			Process( e, GameTime.Zero, light, ol, t );

			ls.SpotLights.Add( light );
			return light;
		}

		
		protected override void Destroy( Entity e, RSSpotLight light )
		{
			ls.SpotLights.Remove( light );
		}

		
		protected override void Process( Entity e, GameTime gameTime, RSSpotLight light, SpotLight spot, Transform t )
		{
			var transform			=	t.TransformMatrix;
			light.Position0			=	transform.TranslationVector + transform.Right * spot.TubeLength * 0.5f;
			light.Position1			=	transform.TranslationVector + transform.Left  * spot.TubeLength * 0.5f;

			light.Intensity			=	spot.LightColor.ToColor4() *  MathUtil.Exp2( spot.LightIntensity );
			light.RadiusInner		=	spot.TubeRadius;
			light.RadiusOuter		=	spot.OuterRadius;

			light.ViewMatrix		=	Matrix.Invert( transform );
			light.ProjectionMatrix	=	spot.ComputeSpotMatrix();
			light.ShadowMaskName	=	spot.SpotMaskName;

			light.EnableGI			=	spot.EnableGI;
			light.LodBias			=	spot.LodBias;
		}
	}
}
