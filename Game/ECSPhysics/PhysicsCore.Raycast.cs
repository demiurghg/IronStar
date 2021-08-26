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
using BEPUphysics.CollisionRuleManagement;
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
	public interface IRaycastCallback
	{
		void Begin( int count );
		bool RayHit( int index, Entity entity, Vector3 location, Vector3 normal, bool isStatic );
		void End();
	}


	public partial class PhysicsCore
	{
		public void Raycast( Ray ray, float maxDistance, IRaycastCallback callback, RaycastOptions options )
		{
			queryRequests.Enqueue( new DeferredRaycast(ray, maxDistance, callback, options) );
		}


		class DeferredRaycast : ISpaceQuery
		{
			readonly Ray ray;
			readonly float maxDistance;
			readonly IRaycastCallback callback;
			readonly RaycastOptions options;  

			List<RayCastResult> results;
			
			public DeferredRaycast ( Ray ray, float maxDistance, IRaycastCallback callback, RaycastOptions options )
			{
				this.ray			=	ray;
				this.callback		=	callback;
				this.maxDistance	=	maxDistance;
				this.options		=	options;
			}

			public void Execute( Space space )
			{
				results				=	new List<RayCastResult>(10);

				var ray = MathConverter.Convert( this.ray );
				space.RayCast( ray, maxDistance, results );

				if (options.HasFlag( RaycastOptions.SortResults ))
				{
					results.Sort( RayCastResultComparison );
				}
			}

			public void Callback()
			{
				callback.Begin( results.Count );

				for ( int idx = 0; idx < results.Count; idx++ )
				{											
					var result		=	results[idx];
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

				callback.End();
			}
		}
	}
}
