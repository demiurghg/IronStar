﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;
using IronStar.SFX;
using Fusion.Engine.Graphics;
using Fusion.Core.Mathematics;

namespace IronStar.ECSGraphics
{
	class BillboardSystem : StatelessSystem<Transform, BillboardComponent>
	{
		readonly FXPlayback fxPlayback;

		Particle particle;
		
		public BillboardSystem( FXPlayback fxPlayback )
		{
			this.fxPlayback	=	fxPlayback;
		}

		protected override void Process( Entity entity, GameTime gameTime, Transform transform, BillboardComponent billboard )
		{
			CreateParticle( ref particle, billboard, transform );
			fxPlayback.rw.ParticleSystem.InjectParticle( particle );
		}


		void CreateParticle( ref Particle p, BillboardComponent b, Transform t )
		{
			p.Effects		=	b.Effect;

			p.ImageIndex	=	0;
			p.ImageCount	=	1;
				
			p.WeaponIndex	=	false;

			byte alpha		=	(byte)(MathUtil.Clamp( b.Alpha, 0, 1 ) * 255);
			p.Color			=	new Color( b.Color.R, b.Color.G, b.Color.B, alpha );
			p.Exposure		=	b.Exposure;
			p.Roughness		=	b.Roughness;
			p.Metallic		=	b.Metallic;
			p.Intensity		=	MathUtil.Exp2( b.IntensityEV );
			p.Scattering	=	b.Scattering;
			p.BeamFactor	=	0;

			p.LifeTime		=	1.0f;
			p.TimeLag		=	2.0f; // make timelad > lifetime to make particle living only for one frame

			p.FadeIn		=	0.1f;
			p.FadeOut		=	0.1f;

			p.Rotation0     =   b.Rotation;
			p.Rotation1     =   b.Rotation;

			p.Size0         =   b.Size;
			p.Size1         =   b.Size;

			p.Position		=	t.Position;

			p.Velocity		=	Vector3.Zero;

			p.Damping		=	0;
			p.Gravity		=	0;
		}
	}
}
