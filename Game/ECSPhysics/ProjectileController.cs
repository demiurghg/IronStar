using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.UpdateableSystems;
using BEPUutilities;
using BEPUphysics;

namespace IronStar.ECSPhysics
{
	class ProjectileController : Updateable, IBeforeSolverUpdateable
	{
		int updateCounter = 0;

		public Vector3 Position { get { return position; } }
		public Vector3 Direction { get { return direction; } }
		public Vector3 LinearVelocity { get { return Direction * velocity; } }

		Vector3 position;
		Vector3 direction;
		readonly float velocity;
		readonly Func<BroadPhaseEntry,bool> filter;
		bool objectHit = false;

		
		public ProjectileController ( Vector3 position, Vector3 direction, float velocity, Func<BroadPhaseEntry, bool> filter )
		{
			this.IsUpdatedSequentially	=	false;
			this.position	=	position;
			this.direction	=	direction;
			this.direction.Normalize();
			this.velocity	=	velocity;
			this.filter		=	filter;
		}

		
		void IBeforeSolverUpdateable.Update( float dt )
		{
			float maxDistance = dt * velocity;
			RayCastResult result;
			
			if (!objectHit)
			{
				if (Space.RayCast( new Ray( position, direction ), maxDistance, filter, out result ))
				{
					position	=	result.HitData.Location;
					var normal	=	result.HitData.Normal;
						normal.Normalize();

					CollisionDetected?.Invoke( this, new CollisionDetectedEventArgs(result.HitObject, position, normal) );

					objectHit = true;
				}
				else
				{
					position	=	position + direction * maxDistance;
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
