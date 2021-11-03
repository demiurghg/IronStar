using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class ProjectileComponent : IComponent
	{
		public Vector3		Origin;
		public Quaternion	Rotation;
		public Vector3		Direction;
		public float		DeltaTime;

		public Entity		Attacker;
		public int			Damage;
		public float		Impulse;

		public string		ExplosionFX;
		public float		Velocity;
		public float		Radius;
		public float		LifeTime;

		public ProjectileComponent()
		{
		}

		/// <summary>
		/// Creates propelled projectile, like rocket or plasma ball
		/// </summary>
		public ProjectileComponent( Entity attacker, Vector3 origin, Quaternion rotation, Vector3 dir, float dt, float velocity, float radius, float lifetime, string explosionFX, int damage, float impulse )
		{
			Origin		=	origin;
			Direction	=	dir.Normalized();
			Rotation	=	rotation;
			DeltaTime	=	dt;
			Attacker	=	attacker;
			Velocity	=	velocity;
			Radius		=	radius;
			LifeTime	=	lifetime;
			ExplosionFX	=	explosionFX;
			Impulse		=	impulse;
			Damage		=	damage;
		}

		/// <summary>
		/// Creates passive projectile, like grenade.
		/// Position is controlled by transform component
		/// </summary>
		public ProjectileComponent( Entity attacker, float radius, float lifetime, string explosionFX, int damage, float impulse )
		{
			Origin		=	Vector3.Zero;
			Direction	=	Vector3.Zero;
			DeltaTime	=	0;
			Attacker	=	attacker;
			Velocity	=	0;
			Radius		=	radius;
			LifeTime	=	lifetime;
			ExplosionFX	=	explosionFX;
			Impulse		=	impulse;
			Damage		=	damage;
		}


		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( Origin		);
			writer.Write( Rotation		);
			writer.Write( Direction		);
			writer.Write( DeltaTime		);

			writer.WriteEntity( gs, Attacker );
			writer.Write( Damage		);
			writer.Write( Impulse		);

			writer.Write( ExplosionFX	);
			writer.Write( Velocity		);
			writer.Write( Radius		);
			writer.Write( LifeTime		);
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Origin		=	reader.Read<Vector3>();
			Rotation	=	reader.Read<Quaternion>();
			Direction	=	reader.Read<Vector3>();
			DeltaTime	=	reader.ReadSingle();

			Attacker	=	reader.ReadEntity(gs);
			Damage		=	reader.ReadInt32();
			Impulse		=	reader.ReadSingle();

			ExplosionFX	=	reader.ReadString();
			Velocity	=	reader.ReadSingle();
			Radius		=	reader.ReadSingle();
			LifeTime	=	reader.ReadSingle();
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
