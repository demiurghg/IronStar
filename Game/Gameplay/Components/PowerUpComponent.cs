using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class PowerupComponent : IComponent
	{
		public int Health;
		public int Armor;

		public PowerupComponent()
		{
		}

		public PowerupComponent( int health, int armor )
		{
			if (health<0) throw new ArgumentOutOfRangeException("health < 0");
			if (armor<0)  throw new ArgumentOutOfRangeException("armor < 0");

			Health	=	health;
			Armor	=	armor;
		}

		public void Load( GameState gs, Stream stream ) {}
		public void Save( GameState gs, Stream stream ) {}
	}
}
