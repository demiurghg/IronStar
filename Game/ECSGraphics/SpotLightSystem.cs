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

		
		protected override void Process( Entity e, GameTime gameTime, RSSpotLight light, SpotLight ol, Transform t )
		{
			var transform		=	t.TransformMatrix;
			light.Position0		=	transform.TranslationVector + transform.Right * ol.TubeLength * 0.5f;
			light.Position1		=	transform.TranslationVector + transform.Left  * ol.TubeLength * 0.5f;

			light.Intensity		=	ol.LightColor.ToColor4() *  MathUtil.Exp2( ol.LightIntensity );
			light.RadiusInner	=	ol.TubeRadius;
			light.RadiusOuter	=	ol.OuterRadius;

			light.SpotView		=	Matrix.Invert( transform );
			light.Projection	=	ol.ComputeSpotMatrix();
			light.SpotMaskName	=	ol.SpotMaskName;

			light.EnableGI		=	ol.EnableGI;
			light.SlopeBias		=	ol.SlopeBias;
			light.DepthBias		=	ol.DepthBias;
			light.LodBias		=	ol.LodBias;
		}
	}
}
