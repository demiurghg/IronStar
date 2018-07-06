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
using BEPUphysics.Character;
using BEPUCharacterController = BEPUphysics.Character.CharacterController;
using Fusion.Core.IniParser.Model;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.PositionUpdating;
using BEPUphysics.PositionUpdating;

namespace IronStar.Physics {
	public class RigidBox {

		readonly Space space;
		readonly Entity entity;
		readonly GameWorld world;
		readonly Box box;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="world"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="depth"></param>
		/// <param name="mass"></param>
		public RigidBox ( Entity entity, GameWorld world, float width, float height, float depth, float mass )
		{
			this.entity		=	entity;
			this.space		=	world.PhysSpace;
			this.world		=	world;

			var ms			=	new MotionState();
			ms.Orientation	=	MathConverter.Convert( Quaternion.Identity );
			ms.Position		=	MathConverter.Convert( Vector3.Zero );
			box				=	new Box( ms, width, height, depth, mass );
			box.PositionUpdateMode	=	PositionUpdateMode.Continuous;

			box.Tag			=	entity;

			box.CollisionInformation.Events.InitialCollisionDetected += Events_InitialCollisionDetected;

			space.Add( box );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="other"></param>
		/// <param name="pair"></param>
		private void Events_InitialCollisionDetected( EntityCollidable sender, Collidable other, CollidablePairHandler pair )
		{
			world.Physics.HandleTouch( pair );
		}


		/// <summary>
		/// 
		/// </summary>
		public void Destroy ()
		{
			space.Remove( box );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="position"></param>
		/// <param name="orient"></param>
		public void Teleport ( Vector3 position, Quaternion orient )
		{
			box.Position		=	MathConverter.Convert( position );
			box.Orientation		=	MathConverter.Convert( orient );
			box.AngularVelocity	=	MathConverter.Convert( Vector3.Zero );
			box.LinearVelocity	=	MathConverter.Convert( Vector3.Zero );
		}


		public void Kick ( Vector3 kickImpulse, Vector3 kickPoint )
		{
			var i = MathConverter.Convert( kickImpulse );
			var p = MathConverter.Convert( kickPoint );
			box.ApplyImpulse( p, i );
		}


		public Vector3 Position {
			get {
				return MathConverter.Convert( box.Position );
			}
		}


		public Quaternion Orientation {
			get {
				return MathConverter.Convert( box.Orientation );
			}
		}


		public Vector3 LinearVelocity {
			get {
				return MathConverter.Convert( box.LinearVelocity );
			}
		}


		public Vector3 AngularVelocity {
			get {
				return MathConverter.Convert( box.AngularVelocity );
			}
		}

	}
}
