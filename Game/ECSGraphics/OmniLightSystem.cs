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
using Fusion.Core;

namespace IronStar.SFX2
{
	public class OmniLightSystem : ProcessingSystem<RSOmniLight,OmniLight,Transform>
	{
		Dictionary<uint,RSOmniLight> lights = new Dictionary<uint, RSOmniLight>();

		readonly LightSet ls;

		
		public OmniLightSystem( RenderSystem rs )
		{
			this.ls	=	rs.RenderWorld.LightSet;
		}

		
		public override RSOmniLight Create( Entity e, OmniLight ol, Transform t )
		{
			var light = new RSOmniLight();

			Process( e, GameTime.Zero, light, ol, t );

			ls.OmniLights.Add( light );
			return light;
		}

		
		public override void Destroy( Entity e, RSOmniLight light )
		{
			ls.OmniLights.Remove( light );
		}

		
		public override void Process( Entity e, GameTime gameTime, RSOmniLight light, OmniLight ol, Transform t )
		{
			var transform		=	t.TransformMatrix;
			light.Position0		=	transform.TranslationVector + transform.Right * ol.TubeLength * 0.5f;
			light.Position1		=	transform.TranslationVector + transform.Left  * ol.TubeLength * 0.5f;

			light.Intensity		=	ol.LightColor.ToColor4() *  MathUtil.Exp2( ol.LightIntensity );
			light.RadiusInner	=	ol.TubeRadius;
			light.RadiusOuter	=	ol.OuterRadius;
		}
	}
}
