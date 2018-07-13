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
	public partial class Player : Entity, IShooter {

		public int Health { get { return health; } }
		public int Armor  { get { return health; } }

		CharacterController	controller;
		int					health;
		int					armor;

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

			 health		=	factory.MaxHealth;
			 armor		=	factory.MaxArmor;

			 //	temp stuff :
			 var weap1	=	world.SpawnItem("weapon_machinegun", ID);
			 var weap2	=	world.SpawnItem("weapon_plasmagun", ID);
		}



		public override void Read( BinaryReader reader, float lerpFactor )
		{
			base.Read( reader, lerpFactor );

			health	=	reader.ReadInt32();
			armor	=	reader.ReadInt32();			
		}


		public override void Write( BinaryWriter writer )
		{
			base.Write( writer );
			
			writer.Write( health );
			writer.Write( armor );
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
			int damageHealth	=	damage / 3;
			int damageArmor		=	damage - damageHealth;

			if (armor>=damageArmor) {
				armor  -= damageArmor;
				health -= damageHealth;
			} else {
				damageArmor  = armor;
				damageHealth = damage - damageArmor;
				armor	=	0;
				health	-=	damageHealth;
			}

			if (health<=0) {
				Log.Warning("KILL!!!!!!!!");
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update ( GameTime gameTime )
		{
			//	update physical character controller :
			controller.Update();

			//	update player's entity states :
			if (controller.Crouching) {
				EntityState |=	EntityState.Crouching;
			} else {
				EntityState &= ~EntityState.Crouching;
			}
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
			Rotation	=	Quaternion.RotationYawPitchRoll( userCommand.Yaw, userCommand.Pitch, userCommand.Roll );

			controller.Move( userCommand.MoveForward, userCommand.MoveRight, userCommand.MoveUp );

			var weapon = World.Items
						.GetOwnedItems( ID )
						.FirstOrDefault( item => item is Weapon ) as Weapon;

			if (userCommand.Action.HasFlag( UserAction.Attack ) ) {
				weapon?.Attack( this, this );
			}

			/*if ( userCommand.Weapon != 0 ) {
				switch (userCommand.Weapon) {
					case 1: pendingWeapon	=	"weapon_machinegun"	; break;
					case 2: pendingWeapon	=	"weapon_plasmagun"	; break;
					case 3: pendingWeapon	=	"machinegun"		; break;
					case 4: pendingWeapon	=	"machinegun"		; break;
					case 5: pendingWeapon	=	"machinegun"		; break;
					case 6: pendingWeapon	=	"machinegun"		; break;
					case 7: pendingWeapon	=	"machinegun"		; break;
					case 8: pendingWeapon	=	"machinegun"		; break;
					default: pendingWeapon	=	"";	break;
				}
			} */
		}



		public Vector3 GetActualPOV()
		{
			float height = EntityState.HasFlag(EntityState.Crouching) ? GameConfig.PovHeightCrouch : GameConfig.PovHeightStand;
			return Position + Vector3.Up * height;
		}


		public Vector3 GetVisiblePOV()
		{
			return GetActualPOV();
		}
	}
}
