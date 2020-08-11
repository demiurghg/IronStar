using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	class ArmamentComponent : IComponent
	{
		public uint WeaponID { get; set; }
		public int[] Ammo;

		public ArmamentComponent()
		{
			WeaponID	=	0;
			Ammo		=	new int[ Enum.GetValues(typeof(AmmoType)).Length ];
		}

		public void Load( GameState gs, Stream stream )	{}
		public void Save( GameState gs, Stream stream )	{}
	}
}
