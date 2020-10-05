using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class WeaponComponent : IComponent
	{
		public string	WeaponAsset;

		public int		HudAmmo;
		public int		HudAmmoMax;

		public TimeSpan Timer;
		public WeaponState State;
		public bool rqAttack;
		public int Counter;

		public WeaponComponent( string weaponAsset )
		{
			WeaponAsset	=	weaponAsset;
		}

		public void Load( GameState gs, Stream stream )	{}
		public void Save( GameState gs, Stream stream )	{}
	}
}
