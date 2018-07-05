﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Extensions;
using IronStar.Core;
using IronStar.SFX;
using System.ComponentModel;
using Fusion.Core.Shell;
using Fusion.Core;
using Fusion;
using IronStar.Items;

namespace IronStar.Entities {

	public class FuncShooter : Entity, IShooter {
		
		static Random rand = new Random();

		readonly Weapon weapon;
		readonly bool trigger;
		readonly bool once;
		bool enabled;

		float cooldown = 0;

		int activationCount = 0;

		public FuncShooter( uint id, short clsid, GameWorld world, FuncShooterFactory factory ) : base(id, clsid, world, factory)
		{
			weapon	=	World.Content.Load( @"weapon\" + factory.Weapon, (Weapon)null );
			trigger	=	factory.Trigger;
			once	=	factory.Once;
			enabled	=	factory.Start;

			if (weapon==null) {
				Log.Warning("FuncShooter : bad weapon : {0}", factory.Weapon );
			}
		}


		public override void Activate( Entity activator )
		{
			if (once && activationCount>0) {
				return;
			}

			if (weapon==null) {
				return;
			}

			activationCount ++;

			if (trigger) {
				if (weapon.Fire( this, World )) {
					Log.Verbose("FuncShooter: fire on trigger");
				}
			} else {
				enabled = !enabled;
				Log.Verbose("FuncShooter: toggle enabled");
			}
		}


		public override void Update( GameTime gameTime )
		{
			float elapsedTime = gameTime.ElapsedSec;

			if (cooldown>0) {
				cooldown -= elapsedTime;
			}

			if (weapon==null) {
				return;
			}

			if (!trigger) {
				if (enabled) {
					weapon.Fire( this, World );
					Log.Verbose("FuncShooter: fire auto");
				}
			}
		}


		public override void Kill()
		{
			base.Kill();
		}

		
		public bool TrySetCooldown( float cooldown )
		{
			if (this.cooldown>0) {
				return false;
			} else {
				this.cooldown = cooldown;
				return true;
			}
		}


		public bool TryConsumeAmmo( string ammoClassname, short count )
		{
			return true;
		}


		public Vector3 GetWeaponPOV(bool useViewOffset)
		{
			return Position;
		}
	}



	/// <summary>
	/// 
	/// </summary>
	public class FuncShooterFactory : EntityFactory {

		[AEClassname("weapon")]
		public string Weapon { get; set; } = "";

		public bool Trigger { get; set; }

		public bool Once { get; set; }

		public bool Start { get; set; }



		public override Entity Spawn( uint id, short clsid, GameWorld world )
		{
			return new FuncShooter( id, clsid, world, this );
		}
	}
}
