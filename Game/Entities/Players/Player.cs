﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using BEPUphysics;
using Fusion.Core.IniParser.Model;
using IronStar.Physics;
using IronStar.Items;

namespace IronStar.Entities.Players {
	public partial class Player : Entity {

		CharacterController	controller;
		Item				pendingItem;	// do not save

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="space"></param>
		public Player ( uint id, short clsid, GameWorld world, PlayerFactory factory ) : base(id,clsid,world,factory)
		{
			controller	=	new CharacterController( this, world, 
				factory.Height,
				factory.CrouchingHeight,
				factory.Radius,
				factory.StandingSpeed,
				factory.CrouchingSpeed,
				factory.JumpSpeed,
				factory.Mass,
				factory.MaxStepHeight
			);

			Health		=	(short)factory.MaxHealth;
			Armor		=	(short)factory.MaxArmor;

			Model		=	world.Atoms[ factory.Model ];	

			//	temp stuff :
			world.SpawnItem("weapon_machinegun", ID);
			/*world.SpawnItem("weapon_machinegun2", ID);
			world.SpawnItem("weapon_plasmagun", ID);
			world.SpawnItem("weapon_shotgun", ID);
			world.SpawnItem("weapon_rocket_launcher", ID);
			world.SpawnItem("weapon_railgun", ID);*/

			ItemID	=	World.Items.GetOwnedItemByClass( ID, "weapon_machinegun" ).ID;
		}





		/// <summary>
		/// 
		/// </summary>
		/// <param name="targetID"></param>
		/// <param name="attackerID"></param>
		/// <param name="damage"></param>
		/// <param name="kickImpulse"></param>
		/// <param name="kickPoint"></param>
		/// <param name="damageType"></param>
		public override void Damage(Entity attacker, int damage, DamageType damageType, Vector3 kickImpulse, Vector3 kickPoint)
		{
			var e = this;

			if (damage<0) {
				throw new ArgumentOutOfRangeException("damage < 0");
			}

			controller.ApplyImpulse( kickImpulse, kickPoint );

			//	armor could adsorb 2/3 of damage.
			int damageHealth	=	(damage / 3);
			int damageArmor		=	(damage - damageHealth);

			if (Armor>=damageArmor) {
				Armor  -= damageArmor;
				Health -= damageHealth;
			} else {
				damageArmor  = Armor;
				damageHealth = (damage - damageArmor);
				Armor	=	0;
				Health	-=	damageHealth;
			}

			if (Health<=0) {
				Log.Warning("KILL!!!!!!!!");
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update ( GameTime gameTime )
		{
			base.Update(gameTime);

			//	update physical character controller :
			controller.Update();

			//	update player's entity states :
			SetState( EntityState.Crouching,	controller.Crouching );
			SetState( EntityState.HasTraction,	controller.HasTraction );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="position"></param>
		/// <param name="orient"></param>
		public override void Teleport( Vector3 position, Quaternion orient )
		{
			base.Teleport( position, orient );

			controller.Teleport( position, Vector3.Zero );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="userCommand"></param>
		public override void UserControl( UserCommand userCommand )
		{
			var oldDir	=	Matrix.RotationQuaternion( Rotation ).Forward;

			Rotation	=	Quaternion.RotationYawPitchRoll( userCommand.Yaw, userCommand.Pitch, userCommand.Roll );

			var newDir	=	Matrix.RotationQuaternion( Rotation ).Forward;

			var turnDir	=	Vector3.Dot( Vector3.Cross( newDir, oldDir ), Vector3.Up ); 

			controller.Move( userCommand.MoveForward, userCommand.MoveRight, userCommand.MoveUp );

			SetState( EntityState.TurnRight, turnDir > 0 );
			SetState( EntityState.TurnLeft,  turnDir < 0 );

			SetState( EntityState.StrafeRight, userCommand.MoveRight > 0 );
			SetState( EntityState.StrafeLeft,  userCommand.MoveRight < 0 );


			//------------------------------------------
			//	shooting
			//------------------------------------------
			var weapon = World.Items.GetOwnedItemByID( ID, ItemID ) as Weapon;
			
			if (userCommand.Action.HasFlag( UserAction.Attack ) ) {
				weapon?.Attack( this );
			}

			//------------------------------------------
			//	weapon switch
			//------------------------------------------
			if ( userCommand.Weapon != 0 ) {
				var nextWeapon = World.Items.GetOwnedItemByClass( ID, userCommand.Weapon );
				if (nextWeapon!=null && nextWeapon.ID!=ItemID) {
					weapon?.Switch( this, nextWeapon.ID );
				}
			}
		}



		/// <summary>
		/// Gets current player weapon
		/// </summary>
		/// <returns></returns>
		public Weapon GetCurrentWeapon ()
		{
			return World.Items.GetOwnedItemByID( ID, ItemID ) as Weapon;
		}


		public override Vector3 GetActualPOV()
		{
			float height = EntityState.HasFlag(EntityState.Crouching) ? GameConfig.PovHeightCrouch : GameConfig.PovHeightStand;
			return Position + Vector3.Up * height;
		}


		public override Vector3 GetVisiblePOV()
		{
			return GetActualPOV();
		}
	}
}
