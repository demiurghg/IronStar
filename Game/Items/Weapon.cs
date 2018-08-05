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
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Core.Extensions;
using IronStar.Core;
using Fusion.Core;
using System.IO;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;
using IronStar.Entities;
using Fusion.Core.Shell;

namespace IronStar.Items {

	public partial class Weapon : Item {
		
		readonly Random rand = new Random();
		readonly GameWorld world;

		readonly bool	beamWeapon;
		readonly float	beamLength;
		readonly string projectile;
		readonly int	damage;
		readonly float	impulse;
		readonly int	projectileCount;
		readonly float	angularSpread;

		readonly int	timeWarmup	;
		readonly int	timeCooldown;
		readonly int	timeOverheat;
		readonly int	timeReload	;
		readonly int	timeDrop	;
		readonly int	timeRaise	;
		readonly string hitFX;
		readonly string ammoItem;
		readonly string viewModel;

		int timer;
		WeaponState state;
		bool dirty;
		bool rqAttack;
		bool rqActivate;
		uint rqNextWeapon;
		int counter;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="world"></param>
		/// <param name="factory"></param>
		public Weapon( uint id, short clsid, GameWorld world, WeaponFactory factory ) : base(id, clsid)
		{
			this.world		=	world;
				
			beamWeapon		=	factory.BeamWeapon		;
			beamLength		=	factory.BeamLength		;
			projectile		=	factory.Projectile		;
			damage			=	factory.Damage			;
			impulse			=	factory.Impulse			;
			projectileCount	=	factory.ProjectileCount	;
			angularSpread	=	factory.AngularSpread	;

			timeWarmup		=	factory.WarmupTime		;
			timeCooldown	=	factory.CooldownTime	;
			timeOverheat	=	factory.OverheatTime	;
			timeReload		=	factory.ReloadTime		;
			timeDrop		=	factory.DropTime		;
			timeRaise		=	factory.RaiseTime		;
			
			hitFX			=	factory.HitFX			;
			ammoItem		=	factory.AmmoItem		;

			viewModel		=	factory.ViewModel		;

		}



		public override bool Switch(Entity target, uint nextItem)
		{
			rqNextWeapon = nextItem;
			return true;
		}


		public override bool Attack(Entity attacker)
		{
			rqAttack = true;
			return true;
		}


		public override void Update(GameTime gameTime)
		{
			if (timer>0) {
				timer -= gameTime.Milliseconds;
			} else {
				timer = 0;
			}

			var entity = world.Entities.GetEntity(Owner);
			if (entity==null) {
				return;
			}

			//	update FSM twice to 
			//	bypass zero time states:
			UpdateFSM( gameTime, entity );
			UpdateFSM( gameTime, entity );

			rqAttack = false;

			//	update animation state :
			//	actually, even dropped weapon could perform attack!!! :)
			//entity.SetState( EntityState.Weapon_States, false );
			bool visible = !(state==WeaponState.Inactive);

			if (entity.ItemID==ID) {
				entity.WeaponState = state;
				entity.ModelFpv	=	world.Atoms[viewModel];
			}
		}



		void UpdateFSM (GameTime gameTime, Entity entity)
		{
			switch (state) {
				case WeaponState.Idle:	
					if (rqAttack) {
						state = WeaponState.Warmup;	
						dirty = true;
						timer = timeWarmup;
					}
					if (rqNextWeapon!=0) {
						state = WeaponState.Drop;	
						dirty = true;
						timer = timeDrop;
					}
					break;

				case WeaponState.Warmup:	
					if (timer<=0) {
						Fire(entity);

						counter++;
						if ((counter&1)==0) {
							state = WeaponState.Cooldown;	
						} else {
							state = WeaponState.Cooldown2;	
						}

						dirty = true;
						timer = timeCooldown;
					}
					break;


				case WeaponState.Cooldown:	
					if (timer<=0) {
						state = WeaponState.Idle;	
						dirty = true;
						timer = 0;
					}
					break;

				case WeaponState.Cooldown2:	
					if (timer<=0) {
						state = WeaponState.Idle;	
						dirty = true;
						timer = 0;
					}
					break;

				case WeaponState.Reload:		
					break;

				case WeaponState.Overheat:		
					break;

				case WeaponState.Drop:	
					if (timer<=0) {
						entity.ItemID = rqNextWeapon;
						rqNextWeapon  = 0;
						state = WeaponState.Inactive;
						dirty = true;
						timer = 0;
					}
					break;

				case WeaponState.Raise:		
					if (timer<=0) {
						state = WeaponState.Idle;
						dirty = true;
						timer = 0;
					}
					break;

				case WeaponState.NoAmmo:		
					break;

				case WeaponState.Inactive:	
					if (entity.ItemID == ID) {
						state = WeaponState.Raise;
						dirty = true;
						timer = timeRaise;
					}	
					break;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		protected virtual bool Fire ( Entity attacker )
		{
			if (beamWeapon) {

				for (int i=0; i<projectileCount; i++) {
					FireBeam( attacker, world );
				}

				return true;

			} else {

				for (int i=0; i<projectileCount; i++) {
					FireProjectile( attacker, world );
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
		void FireBeam ( Entity attacker, GameWorld world )
		{
			var p = attacker.GetActualPOV();
			var q = attacker.Rotation;
			var d = -GetFireDirection(q);

			Vector3 hitNormal;
			Vector3 hitPoint;
			Entity  hitEntity;

			var r = world.RayCastAgainstAll( p, p + d * beamLength, out hitNormal, out hitPoint, out hitEntity, attacker );

			if (r) {
				world.SpawnFX( hitFX, 0, hitPoint, hitNormal );
				world.InflictDamage( hitEntity, attacker.ID, damage, DamageType.BulletHit, d * impulse, hitPoint );
			}
		}



		/// <summary>
		/// Fires projectile
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="world"></param>
		/// <param name="origin"></param>
		void FireProjectile ( Entity attacker, GameWorld world )
		{
			var e = world.Spawn( projectile ) as Projectile;

			if (e==null) {
				Log.Warning("Unknown projectile class: {0}", projectile);
				return;
			}

			var p = attacker.GetActualPOV();
			var q = attacker.Rotation;
			var d = GetFireDirection(q);

			e.ParentID	=	attacker.ID;
			e.Teleport( p, q );

			e.HitDamage		=	damage;
			e.HitImpulse	=	impulse;

			e.FixServerLag(2/60.0f);

			//world.SpawnFX( "MZBlaster",	attacker.ID, origin );
		}



		/// <summary>
		/// Gets firing direction
		/// </summary>
		/// <param name="rotation"></param>
		/// <returns></returns>
		Vector3 GetFireDirection ( Quaternion rotation )
		{ 
			var spreadVector	= GetSpreadVector( angularSpread );
			var rotationMatrix	= Matrix.RotationQuaternion( rotation );
			return Vector3.TransformNormal( spreadVector, rotationMatrix ).Normalized();
		}



		/// <summary>
		/// Gets radial spread vector
		/// </summary>
		/// <param name="spreadAngle"></param>
		/// <returns></returns>
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

