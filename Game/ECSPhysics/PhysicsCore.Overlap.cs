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
	public partial class PhysicsCore
	{
		public void Overlap( Vector3 origin, float radius, IRaycastCallback callback )
		{
			Query( new DeferredOverlap(origin, radius, callback) );
		}


		class DeferredOverlap : ISpaceQuery
		{
			readonly Vector3 origin;
			readonly float radius;
			readonly IRaycastCallback callback;

			readonly List<RayCastResult> results;
			
			public DeferredOverlap ( Vector3 origin, float radius, IRaycastCallback callback )
			{
				this.origin		=	origin;
				this.callback	=	callback;
				this.radius		=	radius;

				results			=	new List<RayCastResult>(10);
			}

			public void Execute( Space space )
			{
				BU.BoundingSphere	sphere		= new BU.BoundingSphere(MathConverter.Convert(origin), radius);
				SphereShape			sphereShape = new SphereShape(radius);
				BEPUVector3			zeroSweep	= BEPUVector3.Zero;
				BU.RigidTransform	rigidXForm	= new BU.RigidTransform( MathConverter.Convert(origin) );	

				var candidates = PhysicsResources.GetBroadPhaseEntryList();
				space.BroadPhase.QueryAccelerator.BroadPhase.QueryAccelerator.GetEntries(sphere, candidates);
			
				foreach ( var candidate in candidates )	
				{
					BU.RayHit rayHit;
					bool r = candidate.ConvexCast( sphereShape, ref rigidXForm, ref zeroSweep, out rayHit );

					if (r) 
					{
						var rcr = new RayCastResult( rayHit, candidate );
						results.Add( rcr );
					}
				}

				RunCallback();
			}

			void RunCallback()
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
