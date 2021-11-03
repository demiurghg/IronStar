using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class ExplosiveComponent : IComponent
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

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( Initiated		);
			writer.Write( Timeout		);
			writer.Write( Radius		);
			writer.Write( Impulse		);
			writer.Write( Damage		);
			writer.Write( BurningFX		);
			writer.Write( ExplosionFX	);
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Initiated	=	reader.ReadBoolean();
			Timeout		=	reader.ReadSingle();
			Radius		=	reader.ReadSingle();
			Impulse		=	reader.ReadSingle();
			Damage		=	reader.ReadInt32();
			BurningFX	=	reader.ReadString();
			ExplosionFX	=	reader.ReadString();
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float factor )
		{
			return Clone();
		}
	}
}
