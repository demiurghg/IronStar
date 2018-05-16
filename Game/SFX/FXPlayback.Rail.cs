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
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion.Engine.Audio;


namespace IronStar.SFX {
	public partial class FXPlayback : DisposableBase {

		float sin ( float a ) { return (float)Math.Sin(a*6.28f); }
		float cos ( float a ) { return (float)Math.Cos(a*6.28f); }

		Random rand = new Random();

		
		public void RunRailTrailFX ( FXEvent fxEvent )
		{
			var p = new Particle();

			p.TimeLag		=	0;

			var m = Matrix.RotationQuaternion( fxEvent.Rotation );
			var up = m.Up;
			var rt = m.Right;

			int count = Math.Min((int)(fxEvent.Velocity.Length() * 20), 2000);

			//
			//	Overall color
			//

			//
			//	Beam :
			//
			p.ImageIndex	=	GetSpriteIndex("railTrace");

			if (true) {
				var pos		=	fxEvent.Origin;

				p.Position		=	pos;
				p.Size0			=	0.1f;
				p.Size1			=	0.0f;

				p.Rotation0		=	0;
				p.Rotation1		=	0;

				p.Acceleration	=	Vector3.Zero;
				p.Damping		=	0;
				p.Gravity		=	0;
				p.Velocity		=	Vector3.Zero;

				p.LifeTime		=	0.5f;
				p.FadeIn		=	0.1f;
				p.FadeOut		=	0.1f;
				
				throw new NotImplementedException();
 
				rw.ParticleSystem.InjectParticle( p );
			}

		}
	}
}
