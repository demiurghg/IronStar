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
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion.Core.IniParser.Model;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using IronStar.SFX;
using System.Runtime.CompilerServices;
using IronStar.Physics;
//using BEPUphysics.


namespace IronStar.Entities {

	public class RigidBody : Entity {

		static Random rand = new Random();

		readonly DynamicBox box;

		readonly bool	explodeOnTrigger;
		readonly bool	explodeOnDamage;
		readonly short	burningFx;
		readonly short	explosionFx;

		readonly int	explosionDamage;
		readonly float	explosionImpulse;
		readonly float	explosionRadius;
				
		bool burning = false;
		int buringTime = 0;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="space"></param>
		public RigidBody ( uint id, short clsid, GameWorld world, RigidBodyFactory factory ) : base( id, clsid, world, factory )
		{
			var width		=	factory.Width;
			var height		=	factory.Height;
			var depth		=	factory.Depth;
			var mass		=	factory.Mass;
			var model		=	factory.Model;

			explodeOnTrigger	=	factory.Explosive && factory.ExplodeOnTrigger;
			explodeOnDamage		=	factory.Explosive && factory.ExplodeOnDamage;
			Health				=	factory.Health;

			burningFx			=	world.Atoms[ factory.BurningFX ];
			explosionFx			=	world.Atoms[ factory.ExplosionFX ];

			explosionDamage		=	factory.ExplosionDamage;
			explosionImpulse	=	factory.ExplosionImpulse;
			explosionRadius		=	factory.ExplosionRadius;

			int minTime			=	Math.Min( factory.BurningMinTime, factory.BurningMaxTime );
			int maxTime			=	Math.Max( factory.BurningMinTime, factory.BurningMaxTime );
			buringTime			=	rand.Next( minTime, maxTime );

			box					=	new DynamicBox( this, world, width, height, depth, mass );

			this.Model		=	world.Atoms[ model ];
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
		public override void Damage( Entity attacker, int damage, DamageType damageType, Vector3 kickImpulse, Vector3 kickPoint )
		{
			box.Kick( kickImpulse, kickPoint );

			if (explodeOnDamage) {
				burning = true;
				Sfx = burningFx;
			}
		}



		public override void Kill()
		{
			base.Kill();
			box.Destroy();
		}


		public override void Activate( Entity activator )
		{
			if (explodeOnTrigger) {
				Explode();
			}
		}



		void Explode ()
		{
			var list = World.WeaponOverlap( Position, explosionRadius, this );

			World.SpawnFX( World.Atoms[explosionFx], 0, Position );
			
			foreach (var ent in list) {
				var dir  = ent.Position - Position;
				var dirN = dir.Normalized();
				var dist = dir.Length();
				var torq = rand.NextVector3( -Vector3.One, Vector3.One );
				ent.Damage( this, explosionDamage, DamageType.RocketExplosion, dirN * explosionImpulse, ent.Position + torq );
			} 

			World.Kill(ID);
		}



		public override bool AllowUse {
			get {
				return true;
			}
		}


		public override void Use( Entity user )
		{
			Log.Message("Box used");
		}


		public override void Update ( GameTime gameTime )
		{
			base.Update(gameTime);

			this.Position			=	box.Position; 
			this.Rotation			=	box.Orientation; 
			this.LinearVelocity		=	box.LinearVelocity;
			this.AngularVelocity	=	box.AngularVelocity;

			if (burning) {
				buringTime -= (int)(gameTime.ElapsedSec * 1000);
				if (buringTime<0) {
					Explode();
				}
			}
		}



		public override void Teleport( Vector3 position, Quaternion orient )
		{
			base.Teleport( position, orient );
			box.Teleport( position, orient );
		}
	}
}
