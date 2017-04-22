using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Core.Extensions;
using IronStar.Core;
using Fusion.Engine.Storage;
using System.IO;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;

namespace IronStar.Items {

	public partial class Weapon : Item {

		interface IState {
			void Enter ( Weapon weapon );
			void Update( Weapon weapon, int dt );
		}



		/// <summary>
		/// Weapon is no activated
		/// </summary>
		class Inactive : IState {

			public void Enter( Weapon weapon )
			{
				weapon.weaponState	=	WeaponState.Inactive;
				weapon.timer.Stop();
			}

			public void Update( Weapon weapon, int dt )
			{
				if (weapon.rqActivation) {
					weapon.rqActivation	= false;
					weapon.SetState( stActivation );
				}
			}
		}



		/// <summary>
		/// Weapon is ready and 
		/// </summary>
		class Ready : IState {

			public void Enter( Weapon weapon )
			{
				weapon.weaponState	=	WeaponState.Idle;
			}

			public void Update( Weapon weapon, int dt )
			{
				if (weapon.rqAttack) {
					//weapon.rqAttack = false;
					weapon.SetState( stWarmup );
				} else {
					weapon.timer.Stop();
				}

				if (weapon.rqNextWeapon!=0) {
					weapon.SetState( stDeactivation );
				}
			}
		}



		/// <summary>
		/// Weapon is ready and 
		/// </summary>
		class Warmup : IState {

			public void Enter( Weapon weapon )
			{
				weapon.weaponState	=	WeaponState.Warmup;

				if (weapon.factory.WarmupPeriod<=0) {
					weapon.FireProjectile();
					weapon.SetState( stRecoil );
				} else {
					weapon.timer.Restart( weapon.factory.WarmupPeriod );
				}
			}

			public void Update( Weapon weapon, int dt )
			{
				if (weapon.timer.Trigger(dt)) {
					weapon.FireProjectile();
					weapon.SetState( stRecoil );
				}
			}
		}



		/// <summary>
		/// Weapon is ready and 
		/// </summary>
		class Recoil : IState {

			public void Enter( Weapon weapon )
			{
				weapon.weaponState	=	weapon.shotCount ? WeaponState.Recoil1 : WeaponState.Recoil2;
				weapon.shotCount	=	!weapon.shotCount;
				weapon.timer.Restart( weapon.factory.CooldownPeriod );
			}

			public void Update( Weapon weapon, int dt )
			{
				if (weapon.timer.Trigger(dt)) {
					weapon.SetState( stReady );
				}
			}
		}



		/// <summary>
		/// Weapon is ready and 
		/// </summary>
		class Reloading : IState {

			public void Enter( Weapon weapon )
			{
			}

			public void Update( Weapon weapon, int dt )
			{
			}
		}



		/// <summary>
		/// Weapon is ready and 
		/// </summary>
		class Empty : IState {

			public void Enter( Weapon weapon )
			{
			}

			public void Update( Weapon weapon, int dt )
			{
			}
		}



		/// <summary>
		/// Weapon is ready and 
		/// </summary>
		class Activation : IState {

			public void Enter( Weapon weapon )
			{
				weapon.timer.Restart( weapon.factory.ActivationPeriod );
			}

			public void Update( Weapon weapon, int dt )
			{
				if (weapon.timer.Trigger(dt)) {
					weapon.SetState( stReady );
				}
			}
		}



		/// <summary>
		/// Weapon is being deactivated
		/// </summary>
		class Deactivation : IState {

			public void Enter( Weapon weapon )
			{
				weapon.timer.Restart( weapon.factory.DeactivationPeriod );
			}

			public void Update( Weapon weapon, int dt )
			{
				if (weapon.timer.Trigger(dt)) {
					weapon.SetState( stInactive );
				}
			}
		}
	}
}
