using System;
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


namespace IronStar.Entities.Players {
	public partial class Player : Entity, IShooter {

		CharacterController	controller;
		Inventory			inventory;
		int					health;
		int					armor;

		public Vector3		ViewPosition;

		float cooldown;

		
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

			 inventory	=	new Inventory();

			 health		=	factory.MaxHealth;
			 armor		=	factory.MaxArmor;
		}



		public override void Read( BinaryReader reader, float lerpFactor )
		{
			base.Read( reader, lerpFactor );
			EntityState	=	(EntityState)reader.ReadByte();
			
		}


		public override void Write( BinaryWriter writer )
		{
			base.Write( writer );
			writer.Write( (byte)EntityState );
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
		public override void Damage(Entity attacker, short damage, DamageType damageType, Vector3 kickImpulse, Vector3 kickPoint)
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

			//	decrease cooldown.
			//	reset cooldown to zero only on next frame!
			if (cooldown>0) {
				cooldown -= gameTime.ElapsedSec;
			} else if (cooldown<0) {
				cooldown = 0;
			}

			if (controller.Crouching) {
				EntityState |=	EntityState.Crouching;
			} else {
				EntityState &= ~EntityState.Crouching;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="userCommand"></param>
		public override void UserControl( UserCommand userCommand )
		{
			Rotation	=	Quaternion.RotationYawPitchRoll( userCommand.Yaw, userCommand.Pitch, userCommand.Roll );

			controller.Move( userCommand.MoveForward, userCommand.MoveRight, userCommand.MoveUp );
		}



		public bool TrySetCooldown( float cooldown )
		{
			if (this.cooldown>0) {
				return false;
			} else {
				this.cooldown += cooldown;
				return true;
			}
		}



		public bool TryConsumeAmmo( string ammoClassname, short count )
		{
			return true;
		}


		public Vector3 GetWeaponPOV()
		{
			float height = EntityState.HasFlag(EntityState.Crouching) ? GameConfig.PovHeightCrouch : GameConfig.PovHeightStand;
			return Position + Vector3.Up * health;
		}
	}
}
