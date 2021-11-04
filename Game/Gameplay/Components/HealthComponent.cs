using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public enum HealthStatus
	{
		Alive,
		JustDied,
		Dead,
	}

	public class HealthComponent : IComponent
	{
		public int MaxHealth = 100;
		public int MaxArmor  = 100;

		public int Health { get; set; }
		public int Armor { get; set; }

		public int LastDamage { get; set; }
		public Entity LastAttacker { get; set; }

		public string Action;

		Entity attacker;
		int accumulatedDamage;

		public HealthComponent()
		{
		}

		public HealthComponent( int health, int armor )
		{
			Health	=	health;
			Armor	=	armor;
		}

		public HealthComponent( int health, int armor, string action )
		{
			Health	=	health;
			Armor	=	armor;
			Action	=	action;
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

		public IComponent Interpolate( IComponent previous, float factor )
		{
			return Clone();
		}

		/*-----------------------------------------------------------------------------------------
		 *	Utils :
		-----------------------------------------------------------------------------------------*/

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
			this.attacker		=	attacker;
			accumulatedDamage	+=	damage;
		}


		public HealthStatus ApplyDamage(bool protect)
		{
			bool wasAlive	=	Health > 0;
			LastDamage		=	accumulatedDamage;
			LastAttacker	=	attacker;

			if (!protect)
			{
				int armorDamage		=	accumulatedDamage * 66 / 100;
				int healthDamage	=	accumulatedDamage - armorDamage;

				Armor	-=	armorDamage;

				if (Armor<0) 
				{
					Health	+=	Armor;
					Armor	=	0;
				}

				Health -= healthDamage;
			}

			accumulatedDamage = 0;
			attacker = null;

			if (wasAlive && Health<=0)
			{
				return HealthStatus.JustDied;
			}

			return Health > 0 ? HealthStatus.Alive : HealthStatus.Dead;
		}
	}
}
