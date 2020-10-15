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
		public readonly int MaxHealth = 100;
		public readonly int MaxArmor  = 100;

		public int Health { get; set; }
		public int Armor { get; set; }

		public int LastDamage { get; set; }

		public string Action;

		int accumulatedDamage;

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

		public void InflictDamage( int damage )
		{
			accumulatedDamage	+=	damage;
		}

		public HealthStatus ApplyDamage(bool protect)
		{
			bool wasAlive	=	Health > 0;
			LastDamage		=	accumulatedDamage;

			if (!protect)
			{
				Health -= accumulatedDamage;
			}

			accumulatedDamage = 0;

			if (wasAlive && Health<=0)
			{
				return HealthStatus.JustDied;
			}

			return Health > 0 ? HealthStatus.Alive : HealthStatus.Dead;
		}

		public void Added( GameState gs, Entity entity ) {	}
		public void Removed( GameState gs )	{	}
		public void Load( GameState gs, Stream stream )	{	}
		public void Save( GameState gs, Stream stream )	{	}
	}
}
