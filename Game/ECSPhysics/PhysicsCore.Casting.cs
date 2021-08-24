﻿using System;
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
	[Flags]
	public enum RaycastOptions
	{
		None,
		SortResults,
	}

	interface ISpaceQuery
	{
		void Execute( Space space );
		void Callback();
	}

	public interface IRaycastCallback
	{
		void Begin( int count );
		bool RayHit( int index, Entity entity, Vector3 location, Vector3 normal, bool isStatic );
		void End();
	}

	public partial class PhysicsCore
	{
		public delegate bool RaycastCallback<T>( int index, Entity entity, T tag, Ray ray, Vector3 location, Vector3 normal, bool isStatic );

		static int RayCastResultComparison ( RayCastResult a, RayCastResult b )
		{
			if ( MathUtil.NearEqual( a.HitData.T, b.HitData.T ) ) return 0;
			else if ( a.HitData.T < b.HitData.T ) return -1; 
			else return 1; 
		}

		readonly ConcurrentQueue<ISpaceQuery> queryRequests = new ConcurrentQueue<ISpaceQuery>();
		readonly ConcurrentQueue<ISpaceQuery> queryResponces = new ConcurrentQueue<ISpaceQuery>();

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
					
					if (!callback.RayHit(idx, entity1 ?? entity2, location, normal, isStatic))
					{
						continue;
					}
				}

				callback.End();
			}
		}


		public void Raycast( Ray ray, float maxDistance, IRaycastCallback callback, RaycastOptions options )
		{
			queryRequests.Enqueue( new DeferredRaycast(ray, maxDistance, callback, options) );
		}


		
		void ExecuteSpatialQueries()
		{
			ISpaceQuery query;

			while ( queryRequests.TryDequeue( out query ) )
			{
				query.Execute( Space );
				queryResponces.Enqueue( query );
			}
		}


		void ExecuteQueryCallbacks()
		{
			ISpaceQuery query;

			while ( queryResponces.TryDequeue( out query ) )
			{
				query.Callback();
			}
		}


		public bool RayCastAgainstEntity ( Vector3 from, Vector3 to, out Vector3 pos, out float distance, out Entity hitEntity )
		{
			lock (Space)
			{
				hitEntity	=	null;
				var dir		=	to - from;
				var dist	=	dir.Length();
				var ndir	=	dir.Normalized();

				distance	=	float.MaxValue;
				Ray ray		=	new Ray( from, ndir );

				pos		= to;

				var rcr		= new RayCastResult();	
				var bRay	= MathConverter.Convert( ray );

				bool result = Space.RayCast( bRay, dist, out rcr );

				if (!result) {
					return false;
				}

				var convex		=	rcr.HitObject as ConvexCollidable;
				pos				=	MathConverter.Convert( rcr.HitData.Location );

				if (convex!=null) {
					hitEntity = convex.Entity.Tag as Entity;
				} 

				distance	=	rcr.HitData.T;

				return true;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="normal"></param>
		/// <param name="pos"></param>
		/// <returns></returns>
		public Entity RayCastEditor ( Vector3 from, Vector3 to, out Vector3 normal, out Vector3 pos, out float distance )
		{
			lock (Space)
			{
				var dir		=	to - from;
				var dist	=	dir.Length();
				var ndir	=	dir.Normalized();
				Ray ray		=	new Ray( from, ndir );

				normal	= Vector3.Zero;
				pos		= to;

				var rcr		= new RayCastResult();	
				var bRay	= MathConverter.Convert( ray );

				bool result = Space.RayCast( bRay, dist, out rcr );

				distance	=	rcr.HitData.T;

				if (!result) {
					return null;
				}

				normal		=	MathConverter.Convert( rcr.HitData.Normal ).Normalized();
				pos			=	MathConverter.Convert( rcr.HitData.Location );

				var convexMesh	=	rcr.HitObject as ConvexCollidable;
				var staticMesh	=	rcr.HitObject as StaticMesh;

				if (convexMesh!=null) {
					return convexMesh.Entity.Tag as Entity;
				}
			
				if (staticMesh!=null) {
					return staticMesh.Tag as Entity;
				}

				return null;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="skipEntity1"></param>
		/// <param name="skipEntity2"></param>
		/// <returns></returns>
		public bool HasLineOfSight( Vector3 from, Vector3 to, Entity skipEntity1, Entity skipEntity2 )
		{
			Vector3 n, p;
			Entity e;
			return !RayCastAgainstAll( from, to, out n, out p, out e, skipEntity1, skipEntity2 );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="normal"></param>
		/// <param name="pos"></param>
		/// <returns></returns>
		public bool RayCastAgainstAll ( Vector3 from, Vector3 to, out Vector3 normal, out Vector3 pos, out Entity hitEntity, Entity skipEntity = null, Entity skipEntity2 = null )
		{
			lock (Space)
			{
				hitEntity	=	null;
				var dir		=	to - from;
				var dist	=	dir.Length();
				var ndir	=	dir.Normalized();
				Ray ray		=	new Ray( from, ndir );

				normal	= Vector3.Zero;
				pos		= to;

				Func<BroadPhaseEntry, bool> filterFunc = delegate(BroadPhaseEntry bpe) 
				{
					if (skipEntity==null) return true;

					if (bpe is StaticCollidable) {

						return true;

					} else if (bpe is ConvexCollidable) {

						var hitEnt = (bpe as ConvexCollidable).Entity.Tag as Entity;

						if (hitEnt==null) {
							return true;
						}

						if (hitEnt==skipEntity) {
							return false;
						}

						if (hitEnt==skipEntity2) {
							return false;
						}

					} else {
						return false;
					}

					ConvexCollidable cc = bpe as ConvexCollidable;
					if (cc==null) return true;
					
					Entity ent = cc.Entity.Tag as Entity;
					if (ent==null) return true;

					if (ent==skipEntity) return false;

					return true;
				};

				var rcr		= new RayCastResult();	
				var bRay	= MathConverter.Convert( ray );

				bool result = Space.RayCast( bRay, dist, filterFunc, out rcr );

				if (!result) {
					return false;
				}

				var convex	=	rcr.HitObject as ConvexCollidable;
				normal		=	MathConverter.Convert( rcr.HitData.Normal ).Normalized();
				pos			=	MathConverter.Convert( rcr.HitData.Location );
				hitEntity	=	(convex == null) ? null : convex.Entity.Tag as Entity;

				return true;
			}
		}


		internal static bool SkipEntityFilter( BroadPhaseEntry bpe, Entity skipEntity )
		{
			if (skipEntity==null) return true;

			if (bpe is StaticCollidable) 
			{
				return true;
			} 
			else if (bpe is ConvexCollidable) 
			{
				var hitEnt = (bpe as ConvexCollidable).Entity.Tag as Entity;

				if (hitEnt==null) 
				{
					return true;
				}

				if (hitEnt==skipEntity) 
				{
					return false;
				}
			}
			else 
			{
				return false;
			}

			return true;
		}


		/// <summary>
		/// Returns the list of ConvexCollidable's and Entities inside or touching the specified sphere.
		/// Result does not include static geometry and non-entity physical objects.
		/// </summary>
		/// <param name="world"></param>
		/// <param name="origin"></param>
		/// <param name="radius"></param>
		/// <returns></returns>
		public List<Entity> WeaponOverlap ( Vector3 origin, float radius, Entity entToSkip )
		{
			lock (Space)
			{
				BU.BoundingSphere	sphere		= new BU.BoundingSphere(MathConverter.Convert(origin), radius);
				SphereShape			sphereShape = new SphereShape(radius);
				BU.Vector3			zeroSweep	= BU.Vector3.Zero;
				BU.RigidTransform	rigidXForm	= new BU.RigidTransform( MathConverter.Convert(origin) );	

				var candidates = PhysicsResources.GetBroadPhaseEntryList();
				Space.BroadPhase.QueryAccelerator.BroadPhase.QueryAccelerator.GetEntries(sphere, candidates);
			
				var result = new List<Entity>();

				foreach ( var candidate in candidates )	{

					BU.RayHit rayHit;
					bool r = candidate.ConvexCast( sphereShape, ref rigidXForm, ref zeroSweep, out rayHit );

					if (r) {
					
						var collidable	=	candidate as ConvexCollidable;
						var entity		=	collidable==null ? null : collidable.Entity.Tag as Entity;

						if (collidable==null) continue;
						if (entity==null) continue;

						result.Add( entity );
					}
				}
			
				result.RemoveAll( e => e == entToSkip );

				return result;
			}
		}

		#if false
		/// <summary>
		/// 
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="normal"></param>
		/// <param name="pos"></param>
		/// <returns></returns>
		public bool RayCastAgainstAll ( Vector3 from, Vector3 to, out Vector3 normal, out Vector3 pos, out Entity hitEnt, Entity entToSkip=null )
		{
			var dir  = to - from;
			var dist = dir.Length();
			var ndir = dir.Normalized();
			Ray ray  = new Ray( from, ndir );

			normal	= Vector3.Zero;
			pos		= to;
			hitEnt	= null;

			Func<BroadPhaseEntry, bool> filterFunc = delegate(BroadPhaseEntry bpe) 
			{
				if (entToSkip==null) return true;

				ConvexCollidable cc = bpe as ConvexCollidable;
				if (cc==null) return true;
					
				Entity ent = cc.Entity.Tag as Entity;
				if (ent==null) return true;

				if (ent==entToSkip) return false;

				return true;
			};
					

			var rcr = new RayCastResult();					

			bool result = Space.RayCast( ray, dist, filterFunc, out rcr );

			if (!result) {
				return false;
			}

			var convex	=	rcr.HitObject as ConvexCollidable;
			normal		=	rcr.HitData.Normal.Normalized();
			pos			=	rcr.HitData.Location;

			hitEnt		=	(convex == null) ? null : convex.Entity.Tag as Entity;

			return true;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="normal"></param>
		/// <param name="pos"></param>
		/// <returns></returns>
		public List<Vector3> RayCastAgainstStatic ( Vector3 from, Vector3 to )
		{							 
			var list = new List<Vector3>();

			var dir  = to - from;
			var dist = dir.Length();
			var ndir = dir.Normalized();
			Ray ray  = new Ray( from, ndir );


			Func<BroadPhaseEntry, bool> filterFunc = delegate(BroadPhaseEntry bpe) 
			{
				ConvexCollidable cc = bpe as ConvexCollidable;
				if (cc!=null) return false;

				return true;
			};

			IList<RayCastResult>	rcrList = new List<RayCastResult>();

			bool result = Space.RayCast( ray, dist, filterFunc, rcrList );

			if (!result) {
				return list;
			}

			foreach ( var rcr in rcrList ) {
				list.Add( rcr.HitData.Location );
				list.Add( rcr.HitData.Normal );
			}

			return list;
		}


		void CHECK( float a ) { Debug.Assert( !float.IsNaN(a) || !float.IsInfinity(a) ); }
		void CHECK( Vector3 a ) { CHECK(a.X); CHECK(a.Y); CHECK(a.Z);  }


		/// <summary>
		/// 
		/// </summary>
		/// <param name="szx"></param>
		/// <param name="szy"></param>
		/// <param name="szz"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="normal"></param>
		/// <param name="pos"></param>
		/// <returns></returns>
		public bool ConvexCastAgainstStatic ( ConvexShape shape, Vector3 from, Vector3 to, out Vector3 normal, out Vector3 pos )
		{
			BoundingBox		bbox;
			Vector3			sweep		= to - from;
			AffineTransform identity	= AffineTransform.Identity;
			RigidTransform	transform	= new RigidTransform( from );

			CHECK( from );
			CHECK( to );

			normal	= Vector3.Zero;
			pos		= to;

            shape.GetSweptLocalBoundingBox( ref transform, ref identity, ref sweep, out bbox );

			var candidates = Resources.GetCollisionEntryList();
            Space.BroadPhase.QueryAccelerator.BroadPhase.QueryAccelerator.GetEntries( bbox, candidates );

			float minT = float.MaxValue;
			bool  hit  = false;
			
			foreach ( var candidate in candidates )	{
				
				if ( candidate as ConvexCollidable != null ) {
					continue;
				}

				RayHit rayHit;
				bool r = candidate.ConvexCast( shape, ref transform, ref sweep, out rayHit );

				if (!r) continue;

				if ( minT > rayHit.T ) {
					hit   	= true;
					minT	= rayHit.T;
					normal	= rayHit.Normal;
					pos		= rayHit.Location;
				}
			}

			return hit;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="szx"></param>
		/// <param name="szy"></param>
		/// <param name="szz"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="normal"></param>
		/// <param name="pos"></param>
		/// <returns></returns>
		public bool ConvexCastAgainstStatic ( ConvexShape shape, Matrix xform, Vector3 initOffset, Vector3 sweep, out Vector3 normal, out Vector3 pos )
		{
			BoundingBox		bbox;
			AffineTransform identity	= AffineTransform.Identity;
			RigidTransform	transform	= new RigidTransform( xform.Translation + initOffset, Quaternion.CreateFromRotationMatrix(xform) );

			normal	= Vector3.Zero;
			pos		= xform.Translation + initOffset + sweep;

            shape.GetSweptLocalBoundingBox( ref transform, ref identity, ref sweep, out bbox );

			var candidates = Resources.GetCollisionEntryList();
            Space.BroadPhase.QueryAccelerator.BroadPhase.QueryAccelerator.GetEntries( bbox, candidates );

			float minT = float.MaxValue;
			bool  hit  = false;
			
			foreach ( var candidate in candidates )	{
				
				if ( candidate as ConvexCollidable != null ) {
					continue;
				}

				RayHit rayHit;
				bool r = candidate.ConvexCast( shape, ref transform, ref sweep, out rayHit );

				if (!r) continue;

				if ( minT > rayHit.T ) {
					hit   	= true;
					minT	= rayHit.T;
					normal	= rayHit.Normal;
					pos		= rayHit.Location;
				}
			}

			return hit;
		}
		#endif
	}
}
