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
		public readonly int MaxHealth = 100;
		public readonly int MaxArmor  = 100;

		public int Health { get; set; }
		public int Armor { get; set; }

		int accumulatedDamage;

		public HealthComponent( int health, int armor )
		{
			Health	=	health;
			Armor	=	armor;
		}

		public void InflictDamage( int damage )
		{
			accumulatedDamage	+=	damage;
		}

		public void ApplyDamage()
		{
			Health -= accumulatedDamage;
			accumulatedDamage = 0;
		}

		public void Added( GameState gs, Entity entity ) {	}
		public void Removed( GameState gs )	{	}
		public void Load( GameState gs, Stream stream )	{	}
		public void Save( GameState gs, Stream stream )	{	}
	}
}
