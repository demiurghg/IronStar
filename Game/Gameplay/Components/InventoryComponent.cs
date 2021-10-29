using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Extensions;
using IronStar.ECS;
using IronStar.Gameplay.Weaponry;

namespace IronStar.Gameplay.Components
{
	public enum InventoryFlags
	{
		None			=	0x00,
		InfiniteAmmo	=	0x01,
	}

	public class InventoryComponent : Component
	{
		readonly int[] ammo		=	new int[ (int)Misc.MaxEnumValue<AmmoType>()   + 1 ];
		readonly int[] weapon	=	new int[ (int)Misc.MaxEnumValue<WeaponType>() + 1 ];

		public InventoryFlags Flags { get { return flags; } }
		InventoryFlags flags;

		public InventoryComponent( InventoryFlags flags = InventoryFlags.None )
		{
			this.flags	=	flags;
		}

		public override IComponent Clone()
		{
			return (InventoryComponent)MemberwiseClone();
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Stuff accessors :
		-----------------------------------------------------------------------------------------------*/

		public bool HasWeapon ( WeaponType weaponType )
		{
			return weapon[ (int)weaponType ] > 0;
		}


		public bool TryGiveWeapon ( WeaponType weaponType, WeaponStateComponent weaponState = null )
		{
			if (weaponType!=WeaponType.None && !HasWeapon(weaponType))
			{
				weapon[ (int)weaponType ]++;
				weaponState?.TrySwitchWeapon(weaponType);
				return true;
			}
			return false;
		}


		public int GetAmmoCount ( AmmoType ammoType )
		{
			return ammo[ (int)ammoType ];
		}


		public bool TryGiveAmmo ( AmmoType ammoType, int count )
		{
			if (count<0) throw new ArgumentOutOfRangeException("count < 0");

			var ammoDesc = Arsenal.Get(ammoType);

			if ( ammo[(int)ammoType] >= ammoDesc.Capacity || count==0 )
			{
				return false;
			}
			else
			{
				ammo[(int)ammoType] += count;
				if (ammo[(int)ammoType] > ammoDesc.Capacity)
				{
					ammo[(int)ammoType] = ammoDesc.Capacity;
				}

				return true;
			}
		}


		public bool TryConsumeAmmo( AmmoType ammoType, int count )
		{
			if (count<0) throw new ArgumentOutOfRangeException("count < 0");

			if (Flags.HasFlag(InventoryFlags.InfiniteAmmo))
			{
				return true;
			}

			if (ammo[ (int)ammoType ] >= count)
			{
				ammo[ (int)ammoType ] -= count;
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
