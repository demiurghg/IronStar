using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class HealthComponent : IComponent
	{
		const int DamageCooldownTimeout = 1000;

		public int MaxHealth = 100;
		public int MaxArmor  = 100;

		public int Health;
		public int Armor;

		public int DamageCooldown;
		public int DamageAccumulator;
		public int LastDamage;
		public Entity LastAttacker;


		public HealthComponent()
		{
		}

		public HealthComponent( int health, int armor )
		{
			Health	=	health;
			Armor	=	armor;
		}

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( MaxHealth	);
			writer.Write( MaxArmor	);
			writer.Write( Health	);
			writer.Write( Armor		);
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			MaxHealth	=	reader.ReadInt32();
			MaxArmor	=	reader.ReadInt32();
			Health		=	reader.ReadInt32();
			Armor		=	reader.ReadInt32();
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}

		/*-----------------------------------------------------------------------------------------
		 *	Utils :
		-----------------------------------------------------------------------------------------*/

		public void RestoreHealth()
		{
			Health = MaxHealth;
		}

		
		public bool TryGiveHealth( int amount )
		{
			if ( Health < MaxHealth && amount > 0 )
			{
				Health = Math.Min( MaxHealth, Health + amount );
				return true;
			}
			else
			{
				return false;
			}
		}

		
		public bool TryGiveArmor( int amount )
		{
			if ( Armor < MaxHealth && amount > 0 )
			{
				Armor = Math.Min( MaxArmor, Armor + amount );
				return true;
			}
			else
			{
				return false;
			}
		}

		
		public void InflictDamage( int damage, Entity attacker )
		{
			LastAttacker		=	attacker;
			DamageAccumulator	+=	damage;
			DamageCooldown		=	DamageCooldownTimeout;
		}
	}
}
