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
using IronStar.Core;
using Fusion.Engine.Audio;


namespace IronStar.SFX {
	public partial class FXPlayback : DisposableBase {

		Random rand = new Random();


		Particle CreateBeam ( FXEvent fxEvent )
		{
			var p = new Particle();

			var m = Matrix.RotationQuaternion( fxEvent.Rotation );
			var dn = m.Down;
			var rt = m.Right;

			var offset		=	0.25f * (dn + rt * 1.2f);

			p.ImageIndex	=	GetSpriteIndex("bulletTrace");

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

				Particle p		=	CreateBeam( fxEvent );

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
			if (true) {

				Particle p		=	CreateBeam( fxEvent );

				p.Size0			=	1.00f;
				p.Size1			=	0.10f;

				p.LifeTime		=	0.4f;
				p.FadeIn		=	0.1f;
				p.FadeOut		=	0.9f;
				
				p.Color			=	new Color(148, 171, 255, 255).ToColor3();
				p.Intensity		=	5000;
				p.Alpha			=	1;

				rw.ParticleSystem.InjectParticle( p );
			}

		}
	}
}
