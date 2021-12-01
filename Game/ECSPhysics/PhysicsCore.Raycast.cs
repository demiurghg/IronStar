using System;
using System.Collections.Generic;
using BEPUphysics;
using System.Linq;
using BEPUVector3 = BEPUutilities.Vector3;
using IronStar.SFX;
using Fusion.Engine.Graphics.Scenes;
using Fusion.Core.Mathematics;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.CollisionRuleManagement;
using IronStar.ECS;
using BU = BEPUutilities;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints.TwoEntity.Motors;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.CollisionShapes;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using Fusion.Core;
using Fusion;
using IronStar.Gameplay;
using Fusion.Core.Extensions;
using BEPUCollisionGroup = BEPUphysics.CollisionRuleManagement.CollisionGroup;
using System.Collections.Concurrent;

namespace IronStar.ECSPhysics
{
	public interface IRaycastCallback<TResult>
	{
		void Begin( int count );
		bool RayHit( int index, Entity entity, Vector3 location, Vector3 normal, bool isStatic );
		TResult End();
	}


	public partial class PhysicsCore
	{
		public T Raycast<T>( Ray ray, float maxDistance, IRaycastCallback<T> callback, RaycastOptions options )
		{
			var raycast = new DeferredRaycast<T>(ray, maxDistance, callback, options);
			return raycast.Execute( Space );
			//queryRequests.Enqueue( raycast );
		}


		class DeferredRaycast<T> : ISpaceQuery<T>
		{
			readonly Ray ray;
			readonly float maxDistance;
			readonly IRaycastCallback<T> callback;
			readonly RaycastOptions options;  

			List<RayCastResult> results;
			
			public DeferredRaycast ( Ray ray, float maxDistance, IRaycastCallback<T> callback, RaycastOptions options )
			{
				this.ray			=	ray;
				this.callback		=	callback;
				this.maxDistance	=	maxDistance;
				this.options		=	options;
			}

			public T Execute( Space space )
			{
				results				=	new List<RayCastResult>(10);

				var ray = MathConverter.Convert( this.ray );
				space.RayCast( ray, maxDistance, results );

				if (options.HasFlag( RaycastOptions.SortResults ))
				{
					results.Sort( RayCastResultComparison );
				}

				return RunCallback();
			}

			T RunCallback()
			{
				callback.Begin( results.Count );

				for ( int idx = 0; idx < results.Count; idx++ )
				{											
					var result		=	results[idx];

					if (result.HitObject is DetectorVolume) continue;

					var entity1		=	(result.HitObject as ConvexCollidable)?.Entity?.Tag as Entity;
					var entity2		=	(result.HitObject as StaticCollidable)?.Tag as Entity;
					var isStatic	=	(result.HitObject is StaticCollidable);
					var location	=	MathConverter.Convert( result.HitData.Location );
					var normal		=	MathConverter.Convert( result.HitData.Normal ).Normalized();
					
					if (callback.RayHit(idx, entity1 ?? entity2, location, normal, isStatic))
					{
						break;
					}
				}

				return callback.End();
			}
		}
	}
}
