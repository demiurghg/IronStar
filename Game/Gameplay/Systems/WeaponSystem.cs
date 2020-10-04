using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;
using IronStar.Gameplay.Components;
using Fusion;
using IronStar.ECSPhysics;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using IronStar.AI;
using IronStar.Gameplay.Assets;

namespace IronStar.Gameplay.Systems
{
	class WeaponSystem : ISystem
	{
		const float BEAM_RANGE	=	8192;

		Random rand = new Random();

		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }
		public readonly PhysicsCore physics;


		Aspect weaponAspect			=	new Aspect().Include<WeaponComponent>();
		Aspect armedEntityAspect	=	new Aspect().Include<InventoryComponent,UserCommandComponent,CharacterController>()
													.Include<Transform>();


		public WeaponSystem( PhysicsCore physics )
		{
			this.physics	=	physics;
		}

		
		public void Update( GameState gs, GameTime gameTime )
		{
			var entities = gs.QueryEntities( armedEntityAspect );

			foreach ( var entity in entities )
			{
				var transform	=	entity.GetComponent<Transform>();
				var inventory	=	entity.GetComponent<InventoryComponent>();
				var userCmd		=	entity.GetComponent<UserCommandComponent>();
				var chctrl		=	entity.GetComponent<CharacterController>();

				var povTransform	=	userCmd.RotationMatrix * Matrix.Translation(transform.Position + chctrl.PovOffset);

				if (inventory.HasPendingWeapon && inventory.ActiveWeaponID==0)
				{
					inventory.FinalizeWeaponSwitch();
				}

				//	tell inventory to switch to another weapon :
				SwitchWeapon( gs, userCmd, inventory );
					
				var activeItem	=	gs.GetEntity( inventory.ActiveWeaponID );

				//	is active item weapon?
				if (weaponAspect.Accept(activeItem))
				{
					var weapon	= activeItem.GetComponent<WeaponComponent>();
					var desc	= gs.Content.Load( weapon.WeaponAsset, WeaponDescription.Default );
					var attack	= userCmd.Action.HasFlag( UserAction.Attack );

					var ammo	= GetAmmo( gs, inventory, desc );

					weapon.HudAmmo		=	ammo==null ? 0 : ammo.Count;
					weapon.HudAmmoMax	=	200;

					AdvanceWeaponTimer( gameTime, weapon );
					UpdateWeaponFSM( gs, gameTime, activeItem.ID, attack, povTransform, entity, inventory, weapon, desc );
				}
			}
		}


		bool SwitchWeapon( GameState gs, UserCommandComponent userCmd, InventoryComponent inventory )
		{
			if (userCmd.Weapon!=null)
			{
				foreach ( var eid in inventory )
				{
					var e = gs.GetEntity(eid); 
					var n = e?.GetComponent<NameComponent>()?.Name;

					if (n==userCmd.Weapon)
					{
						inventory.SwitchWeapon(eid);
						return true;
					}
				}
			}

			return false;
		}


		/*-----------------------------------------------------------------------------------------------
		 * Weapon state
		-----------------------------------------------------------------------------------------------*/
		
		void AdvanceWeaponTimer( GameTime gameTime, WeaponComponent weapon )
		{
			if ( weapon.Timer > TimeSpan.Zero ) 
			{
				weapon.Timer = weapon.Timer - gameTime.Elapsed;
			}
		}


		void UpdateWeaponFSM (GameState gs, GameTime gameTime, uint id, bool attack, Matrix povTransform, Entity attacker, InventoryComponent inventory, WeaponComponent weapon, WeaponDescription desc )
		{
			var timeout	=	weapon.Timer <= TimeSpan.Zero;

			switch (weapon.State) 
			{
				case WeaponState.Idle:	
					if (attack) 
					{
						if (TryConsumeAmmo(gs, inventory, weapon, desc)) 
						{
							weapon.State =  WeaponState.Warmup;	
							weapon.Timer += desc.TimeWarmup;
						} 
						else 
						{
							weapon.State =  WeaponState.NoAmmo;	
							weapon.Timer += desc.TimeNoAmmo;
						}
					}
					if (inventory.HasPendingWeapon) 
					{
						weapon.State =  WeaponState.Drop;	
						weapon.Timer =  desc.TimeDrop;
					}
					break;

				case WeaponState.Warmup:	
					if (timeout) 
					{
						Fire(gameTime, weapon, desc, povTransform, attacker);

						weapon.Counter++;
						
						if ((weapon.Counter&1)==0) 
						{
							weapon.State = WeaponState.Cooldown;	
						} 
						else 
						{
							weapon.State = WeaponState.Cooldown2;	
						}

						weapon.Timer += desc.TimeCooldown;
					}
					break;


				case WeaponState.Cooldown:	
					if (timeout) 
					{
						weapon.State = WeaponState.Idle;	
					}
					break;

				case WeaponState.Cooldown2:	
					if (timeout) 
					{
						weapon.State = WeaponState.Idle;	
					}
					break;

				case WeaponState.Reload:		
					break;

				case WeaponState.Overheat:		
					break;

				case WeaponState.Drop:	
					if (timeout) 
					{
						inventory.FinalizeWeaponSwitch();
						weapon.State = WeaponState.Inactive;
						weapon.Timer = TimeSpan.Zero;
					}
					break;

				case WeaponState.Raise:		
					if (timeout) 
					{
						weapon.State = WeaponState.Idle;
						weapon.Timer = TimeSpan.Zero;
					}
					break;

				case WeaponState.NoAmmo:		
					if (timeout) 
					{
						weapon.State = WeaponState.Idle;
						weapon.Timer = TimeSpan.Zero;
					}
					break;

				case WeaponState.Inactive:	
					if (inventory.ActiveWeaponID == id) 
					{
						weapon.State = WeaponState.Raise;
						weapon.Timer = desc.TimeRaise;
					}	
					break;
			}
		}


		AmmoComponent GetAmmo( GameState gs, InventoryComponent inventory, WeaponDescription desc )
		{
			var ammoEntity	=	inventory.FindItem( gs, desc.AmmoClass );
			var ammo		=	ammoEntity?.GetComponent<AmmoComponent>();
			return ammo;
		}


		bool TryConsumeAmmo( GameState gs, InventoryComponent inventory, WeaponComponent weapon, WeaponDescription desc )
		{
			return true;

			var ammo		=	GetAmmo( gs, inventory, desc );

			if (ammo==null) 
			{
				return false;
			}

			if (ammo.Count >= desc.AmmoConsumption)
			{
				ammo.Count -= desc.AmmoConsumption;
				return true;
			}
			else
			{
				return false;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		bool Fire ( GameTime gameTime, WeaponComponent weapon, WeaponDescription desc, Matrix povTransform, Entity attacker )
		{
			var gs = attacker.gs;

			if (desc.IsBeamWeapon) 
			{
				for (int i=0; i<desc.ProjectileCount; i++) 
				{
					FireBeam( gs, weapon, desc, povTransform, attacker );
				}
				return true;
			} 
			else 
			{
				for (int i=0; i<desc.ProjectileCount; i++) 
				{
					FireProjectile( gs, gameTime, weapon, desc, povTransform, attacker );
				}
				return true;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="shooter"></param>
		/// <param name="world"></param>
		void FireBeam ( GameState gs, WeaponComponent weapon, WeaponDescription desc, Matrix povTransform, Entity attacker )
		{
			var p = povTransform.TranslationVector;
			var q = Quaternion.RotationMatrix( povTransform );
			var d = -GetFireDirection( q, desc.Spread );

			Vector3 hitNormal;
			Vector3 hitPoint;
			Entity  hitEntity;

			var r = physics.RayCastAgainstAll( p, p + d * BEAM_RANGE, out hitNormal, out hitPoint, out hitEntity, attacker );

			if (r) 
			{
				SFX.FXPlayback.AttachFX( gs, hitEntity, desc.BeamHitFX, 0, hitPoint, hitNormal );
				PhysicsCore.ApplyImpulse( hitEntity, hitPoint, d * desc.Impulse );
				HealthSystem.ApplyDamage( hitEntity, desc.Damage );
			} 
			else 
			{
				hitPoint = p + d * BEAM_RANGE;
			}

			//	run trail FX:
			var beamOrigin	 =	p;
			var beamVelocity =	hitPoint - p;
			var basis		=	MathUtil.ComputeAimedBasis( d );
			SFX.FXPlayback.SpawnFX(	gs, desc.BeamTrailFX, 0, beamOrigin, beamVelocity, Quaternion.RotationMatrix(basis) );
		}



		/// <summary>
		/// Fires projectile
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="world"></param>
		/// <param name="origin"></param>
		void FireProjectile ( GameState gs, GameTime gameTime, WeaponComponent weapon, WeaponDescription desc, Matrix povTransform, Entity attacker )
		{
			var v = attacker.GetComponent<Velocity>();
			var p = povTransform.TranslationVector + v.Linear * gameTime.ElapsedSec;
			var q = Quaternion.RotationMatrix( povTransform );
			var d = -GetFireDirection( q, desc.Spread );

			var e = gs.Spawn( desc.ProjectileClass );

			var projectile	=	e.GetComponent<ProjectileComponent>();
			var transform	=	e.GetComponent<Transform>();

			projectile.Damage		=	desc.Damage;
			projectile.Impulse		=	desc.Impulse;
			projectile.SenderID		=	attacker.ID;
			projectile.Direction	=	d;

			transform.Position	=	povTransform.TranslationVector;
			transform.Scaling	=	Vector3.One;
			transform.Rotation	=	q;
		}



		Vector3 GetFireDirection ( Quaternion rotation, float spreadAngle )
		{ 
			var spreadVector	= GetSpreadVector( spreadAngle );
			var rotationMatrix	= Matrix.RotationQuaternion( rotation );
			return Vector3.TransformNormal( spreadVector, rotationMatrix ).Normalized();
		}


		Vector3 GetSpreadVector ( float spreadAngle )
		{
			var randomAngle	 = MathUtil.DegreesToRadians( spreadAngle );

			var theta	=	rand.NextFloat( 0, MathUtil.TwoPi );
			var tMin	=	(float)Math.Cos( randomAngle );
			var t		=	rand.NextFloat( tMin, 1 );
			var phi		=	(float)Math.Acos( t );
			var sinPhi	=	(float)Math.Sin( phi );

			var x		=	(float)( Math.Sin( phi ) * Math.Cos( theta ) );
			var y		=	(float)( Math.Sin( phi ) * Math.Sin( theta ) );
			var z		=	-(float)( Math.Cos( phi ) ); // because forward is -Z

			return new Vector3( x, y, -z );
		}
	}
}
