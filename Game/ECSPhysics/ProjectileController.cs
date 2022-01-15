using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.UpdateableSystems;
using BEPUutilities;
using BEPUphysics;
using BEPUphysics.EntityStateManagement;

namespace IronStar.ECSPhysics
{
	class ProjectileController : Updateable, IBeforeSolverUpdateable
	{
		public Vector3 Position { get { return motionState.Position; } }
		public Vector3 LinearVelocity { get { return motionState.LinearVelocity; } }
		public MotionState MotionState { get { return motionState; } }

		readonly Func<BroadPhaseEntry,bool> filter;
		bool objectHit = false;


		MotionState motionState;

		
		public ProjectileController ( Vector3 position, Quaternion orientation, Vector3 velocity, Func<BroadPhaseEntry, bool> filter )
		{
			this.IsUpdatedSequentially	=	false;

			motionState		=	new MotionState();
			motionState.AngularVelocity	=	Vector3.Zero;
			motionState.Orientation		=	orientation;
			motionState.LinearVelocity	=	velocity;
			motionState.Position		=	position;

			this.filter		=	filter;
		}

		
		void IBeforeSolverUpdateable.Update( float dt )
		{
			var maxDistance	=	(dt * motionState.LinearVelocity).Length();

			if (maxDistance==0)
			{
				return;
			}

			RayCastResult result;
			var direction = motionState.LinearVelocity;
			direction.Normalize();
			
			if (!objectHit)
			{
				if (Space.RayCast( new Ray( motionState.Position, direction ), maxDistance, filter, out result ))
				{
					motionState.Position	=	result.HitData.Location;
					var normal				=	result.HitData.Normal;

					if (normal.Length()>0) normal.Normalize();

					CollisionDetected?.Invoke( this, new CollisionDetectedEventArgs(result.HitObject, motionState.Position, normal) );

					objectHit = true;
				}
				else
				{
					motionState.Position	=	motionState.Position + direction * maxDistance;
				}
			}
		}


		public class CollisionDetectedEventArgs : EventArgs
		{
			public CollisionDetectedEventArgs( BroadPhaseEntry hitObject, Vector3 location, Vector3 normal ) 
			{
				HitObject	=	hitObject; 
				Location	=	location;
				Normal		=	normal;
			}

			public readonly BroadPhaseEntry HitObject;
			public readonly Vector3 Location;
			public readonly Vector3 Normal;
		}


		public event EventHandler<CollisionDetectedEventArgs> CollisionDetected;
	}
}
