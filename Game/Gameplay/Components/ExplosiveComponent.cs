using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class ExplosiveComponent : Component
	{
		public bool		Initiated;
		public float	Timeout;
		public float	Radius;
		public float	Impulse;
		public int		Damage;
		public string	BurningFX;
		public string	ExplosionFX;

		public ExplosiveComponent( float timeout, int damage, float radius, float impulse, string burningFX, string explosionFX )
		{
			Initiated	=	false		;
			Timeout		=	timeout		;
			Radius		=	radius		;
			Damage		=	damage		;
			Impulse		=	impulse		;
			BurningFX	=	burningFX	;
			ExplosionFX	=	explosionFX	;
		}
	}
}
