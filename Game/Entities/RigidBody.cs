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
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using IronStar.SFX;
//using BEPUphysics.


namespace IronStar.Entities {

	public enum ReplicateLevel {
		SyncEveryFrame,
		Sync
	}

	public class ReplicateAttribute : Attribute {
		
	}


	public class RigidBody : Entity {

		readonly Space space;
		readonly Box box;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="space"></param>
		public RigidBody ( uint id, short clsid, GameWorld world, RigidBodyFactory factory ) : base( id, clsid, world, factory )
		{
			this.space		=	world.PhysSpace;

			var width		=	factory.Width;
			var height		=	factory.Height;
			var depth		=	factory.Depth;
			var mass		=	factory.Mass;
			var model		=	factory.Model;

			var ms	=	new MotionState();
			ms.Orientation	=	MathConverter.Convert( Quaternion.Identity );
			ms.Position		=	MathConverter.Convert( Vector3.Zero );
			box		=	new Box( ms, width, height, depth, mass );
			box.PositionUpdateMode	=	PositionUpdateMode.Continuous;

			box.Tag	=	this;

			space.Add( box );

			this.Model		=	world.Atoms[ model ];
		}

		Random rand = new Random();



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
			var i = MathConverter.Convert( kickImpulse );
			var p = MathConverter.Convert( kickPoint );
			box.ApplyImpulse( p, i );
		}



		public override void Kill()
		{
			base.Kill();
			space.Remove(box);
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


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update ( GameTime gameTime )
		{
			this.Position			=	MathConverter.Convert( box.Position ); 
			this.Rotation			=	MathConverter.Convert( box.Orientation ); 
			this.LinearVelocity		=	MathConverter.Convert( box.LinearVelocity );
			this.AngularVelocity	=	MathConverter.Convert( box.AngularVelocity );
		}


		public override void Teleport( Vector3 position, Quaternion orient )
		{
			base.Teleport( position, orient );

			box.Position		=	MathConverter.Convert( position );
			box.Orientation		=	MathConverter.Convert( orient );
			box.AngularVelocity	=	MathConverter.Convert( Vector3.Zero );
			box.LinearVelocity	=	MathConverter.Convert( Vector3.Zero );
		}


		public override void Move( Vector3 position, Quaternion orient, Vector3 velocity )
		{
			base.Move( position, orient, velocity );

			box.Position		=	MathConverter.Convert( position );
			box.Orientation		=	MathConverter.Convert( orient );
			box.AngularVelocity	=	MathConverter.Convert( Vector3.Zero );
			box.LinearVelocity	=	MathConverter.Convert( velocity );
		}
	}
}
