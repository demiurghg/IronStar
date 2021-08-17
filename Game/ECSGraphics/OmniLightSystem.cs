﻿using System;
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
	public class OmniLightSystem : ProcessingSystem<RSOmniLight,OmniLight,KinematicState>
	{
		Dictionary<uint,RSOmniLight> lights = new Dictionary<uint, RSOmniLight>();

		readonly LightSet ls;
	
		public OmniLightSystem( RenderSystem rs )
		{
			ls	=	rs.RenderWorld.LightSet;
		}

		
		protected override RSOmniLight Create( Entity e, OmniLight ol, KinematicState t )
		{
			var light = new RSOmniLight();

			Process( e, GameTime.Zero, light, ol, t );

			ls.OmniLights.Add( light );
			return light;
		}

		
		protected override void Destroy( Entity e, RSOmniLight light )
		{
			ls.OmniLights.Remove( light );
		}

		
		protected override void Process( Entity e, GameTime gameTime, RSOmniLight light, OmniLight ol, KinematicState t )
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
