using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Engine.Audio;


namespace IronStar.SFX {
	public partial class FXPlayback {

		float sin ( float a ) { return (float)Math.Sin(a*6.28f); }
		float cos ( float a ) { return (float)Math.Cos(a*6.28f); }


		Particle CreateBeam ( FXEvent fxEvent, float dx, float dy )
		{
			var p = new Particle();

			var m = Matrix.RotationQuaternion( fxEvent.Rotation );
			var dn = m.Down;
			var rt = m.Right;

			var offset		=	dy * dn + dx * rt;

			p.ImageIndex	=	GetSpriteClip("bulletTrace").FirstIndex;
			p.ImageCount	=	GetSpriteClip("bulletTrace").Length;

			p.TimeLag		=	0;
			p.Position		=	fxEvent.Origin + offset + fxEvent.Velocity * 0.5f;
			p.Velocity		=	fxEvent.Velocity * 0.5f - offset * 1f;
			p.BeamFactor	=	-1;
			p.Effects		=	ParticleFX.Soft;

			p.Rotation0		=	0;
			p.Rotation1		=	0;

			p.Acceleration	=	Vector3.Zero;
			p.Damping		=	0;
			p.Gravity		=	0;

			return p;
		}



		public void RunTrailBullet ( FXEvent fxEvent )
		{
			if (true) {

				Particle p		=	CreateBeam( fxEvent, 0.6f, 0.4f );

				p.Size0			=	0.05f;
				p.Size1			=	1.20f;

				p.LifeTime		=	0.05f;
				p.FadeIn		=	0.1f;
				p.FadeOut		=	0.9f;
				
				p.Color			=	new Color(255, 156, 73, 255).ToColor3();
				p.Intensity		=	500;
				p.Alpha			=	1;
 
				rw.ParticleSystem.InjectParticle( p );
			}
		}



		public void RunTrailGauss ( FXEvent fxEvent )
		{
			var p			=	CreateBeam( fxEvent, 0.65f, 0.39f );

			var rayPos			=	p.Position - p.Velocity;
			var rayVel			=	p.Velocity * 2;

			var	color		=	new Color(148, 171, 255, 255).ToColor3();

			p.Size0			=	1.00f;
			p.Size1			=	0.30f;

			p.LifeTime		=	0.5f;
			p.FadeIn		=	0.1f;
			p.FadeOut		=	0.9f;
				
			p.Color			=	color;
			p.Intensity		=	5000;
			p.Alpha			=	1;

			p.ImageIndex	=	GetSpriteClip("bulletTrace").FirstIndex;
			p.ImageCount	=	GetSpriteClip("bulletTrace").Length;

			rw.ParticleSystem.InjectParticle( p );

			//----------------------------------

			int count = Math.Min((int)(fxEvent.Velocity.Length() * 4), 2000);

			var m = Matrix.RotationQuaternion( fxEvent.Rotation );
			var up = m.Up;
			var rt = m.Right;

			p.Effects		=	ParticleFX.Soft;
			//color		=	new Color(255, 171, 148, 255).ToColor3();

			for (int i=0; i<count; i++) {

				var t		=	i * 0.1f;
				var c		=	(float)Math.Cos(t);
				var s		=	(float)Math.Sin(t);

				var factor	=	i / (float)count;

				#if false
				var pos		=	rayPos + rt * c * 0.10f + up * s * 0.10f + rayVel * (i+0)/(float)count;
				var vel		=	rt * c * 0.15f + up * s * 0.15f + rand.GaussRadialDistribution(0,0.5f);
				#else
				var pos		=	rayPos + rt * c * 0.0f + up * s * 0.0f + rayVel * factor;
				var vel		=	rand.GaussRadialDistribution(0,0.5f);
				var accel	=	rand.GaussRadialDistribution(0,3f);
				#endif

				var time	=	rand.GaussDistribution(1, 0.2f);

				p.Position		=	pos;
				p.Velocity		=	vel;
				p.Acceleration	=	accel;
				p.Damping		=	8f;
				p.Gravity		=	0;

				p.BeamFactor	=	0;

				p.TimeLag		=	-0.1f;
				p.LifeTime		=	rand.GaussDistribution(0.5f, 0.3f);

				p.Intensity		=	rand.NextFloat(2000, 5000);
				p.Color			=	color;
				p.Alpha			=	1;
				p.FadeIn		=	0.3f;
				p.FadeOut		=	0.4f;			


				p.ImageIndex	=	GetSpriteClip("railDot").FirstIndex;
				p.ImageCount	=	GetSpriteClip("railDot").Length;

				p.Rotation0		=	rand.NextFloat(0,360);
				p.Rotation1		=	p.Rotation0;
				p.Size0			=	0.3f;
				p.Size1			=	0.0f;

				rw.ParticleSystem.InjectParticle( p );
			}
		}
	}
}
