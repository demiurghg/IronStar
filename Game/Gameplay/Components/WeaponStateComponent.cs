using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;
using IronStar.ECS;
using IronStar.Gameplay.Weaponry;

namespace IronStar.Gameplay.Components
{
	public class WeaponStateComponent : IComponent
	{
		public WeaponState	State			=	WeaponState.Idle;
		public TimeSpan		Timer			=	TimeSpan.Zero;
		public float		Spread			=	0;
		public int			Counter			=	0;

		public WeaponType	ActiveWeapon	=	WeaponType.None;
		public WeaponType	PendingWeapon	=	WeaponType.None;

		public bool HasPengingWeapon { get { return PendingWeapon!=WeaponType.None; } }

		public bool TrySwitchWeapon( WeaponType weapon )
		{
			if (weapon!=WeaponType.None && ActiveWeapon!=weapon)
			{
				PendingWeapon = weapon;
				return true;
			}
			return false;
		}


		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( (int)State	);
			writer.Write( Timer			);
			writer.Write( Spread		);	
			writer.Write( Counter		);	

			writer.Write( (int)ActiveWeapon		);
			writer.Write( (int)PendingWeapon	);
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			State			=	(WeaponState)reader.ReadInt32();
			Timer			=	reader.Read<TimeSpan>();
			Spread			=	reader.ReadSingle();
			Counter			=	reader.ReadInt32();

			ActiveWeapon	=	(WeaponType)reader.ReadInt32();
			PendingWeapon	=	(WeaponType)reader.ReadInt32();
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}
	}
}
