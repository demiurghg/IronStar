using System;
using System.Collections.Generic;
using System.Linq;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.Paths.PathFollowing;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using IronStar.ECS;
using BepuEntity = BEPUphysics.Entities.Entity;
using BEPUVector3 = BEPUutilities.Vector3;

namespace IronStar.ECSPhysics
{
	public class KinematicController
	{
		readonly SceneView<KinematicBody> sceneView;
		readonly AnimationKey[] frame0;
		readonly AnimationKey[] frame1;


		
		public KinematicController( PhysicsCore physics, Entity entity, Scene scene, Matrix transform )
		{
			sceneView = new SceneView<KinematicBody>( scene, (n,m) => new KinematicBody(entity,transform,n,m), n => true );

			frame0	=	new AnimationKey[ sceneView.transforms.Length ];
			frame1	=	new AnimationKey[ sceneView.transforms.Length ];

			sceneView.ForEachMesh( 
				body => 
				{
					physics.Add( body.ConvexHull );
					physics.Add( body.Rotator );
					physics.Add( body.Mover );
				}
			);
		}


		public void Animate( Matrix world, KinematicModel kinematic, Matrix[] dstBones, bool skipSimulation )
		{
			var take = sceneView.scene.Takes.FirstOrDefault();
			var time = kinematic.Time;
			int prev, next;
			float weight;

			Scene.TimeToFrames( time, sceneView.scene.TimeMode, out prev, out next, out weight );

			prev = MathUtil.Wrap( prev + take.FirstFrame, take.FirstFrame, take.LastFrame );
			next = MathUtil.Wrap( next + take.FirstFrame, take.FirstFrame, take.LastFrame );

			take.GetPose( prev, AnimationBlendMode.Override, frame0 ); 
			take.GetPose( next, AnimationBlendMode.Override, frame1 ); 

			for (int idx=0; idx < frame0.Length; idx++)
			{
				frame0[idx] = AnimationKey.Lerp( frame0[idx], frame1[idx], weight );
			}

			//	We need bypass simulation in editor mode.
			//	In this case we just copy animated transforms to bone component
			if (!skipSimulation)
			{
				AnimationKey.CopyTransforms( frame0, sceneView.transforms );
				sceneView.SetTransforms( world, sceneView.transforms, false );
			}
			else
			{
				AnimationKey.CopyTransforms( frame0, dstBones );
				sceneView.scene.ComputeAbsoluteTransforms( dstBones );
			}

			sceneView.ForEachMesh( body => body.Update() );
		}


		public void GetTransform( Matrix invWorld, Matrix[] destination )
		{
			sceneView.ForEachMesh(
				(idx,body) =>
				{
					destination[idx] = body.World * invWorld;
				}
			);
		}


		public void Destroy(PhysicsCore physics)
		{
			sceneView.ForEachMesh( 
				body => 
				{
					physics.Remove( body.ConvexHull );
					physics.Remove( body.Rotator );
					physics.Remove( body.Mover );
				}
			);
		}


		const float MinSquashDotProduct		=	-0.5f;
		const float MaxAllowedPenetration	=	 0.5f;


		public bool SquishTargets( Action<Entity> squishAction )
		{
			bool any = false;

			var candidates = GetSquishCandidates();
			Entity target = null;

			foreach ( var candidate in candidates )
			{
				if (IsSquishing(candidate, out target))
				{
					any |= true;

					if (target!=null)
					{
						squishAction(target);
					}
				}
			}

			return any;
		}


		HashSet<BepuEntity> GetSquishCandidates()
		{
			var candidates = new HashSet<BepuEntity>(10);

			sceneView.ForEachMesh( 
				body =>
				{
					foreach ( var pair in body.ConvexHull.CollisionInformation.Pairs )
					{
						if (pair.EntityA!=null && !body.ConvexHull.Equals(pair.EntityA)) candidates.Add( pair.EntityA );
						if (pair.EntityB!=null && !body.ConvexHull.Equals(pair.EntityB)) candidates.Add( pair.EntityB );
					}
				}
			);

			return candidates;
		}


		bool IsSquishing( BepuEntity physEntity, out Entity entity )
		{
			var normals = new List<Vector4>(10);
			
			entity			=	physEntity.Tag as Entity;

			var totalNormal	=	BEPUVector3.Zero;
			var boxSize		=	physEntity.CollisionInformation.BoundingBox.Max - physEntity.CollisionInformation.BoundingBox.Min;
			var minBoxSize	=	Math.Min( Math.Min( boxSize.X, boxSize.Y ), Math.Min(boxSize.Z, 0.01f) );

			foreach ( var pair in physEntity.CollisionInformation.Pairs )
			{
				float sign = pair.EntityA==physEntity ? 1 : -1;

				foreach ( var contact in pair.Contacts )
				{
					normals.Add( new Vector4( sign * MathConverter.Convert( contact.Contact.Normal ).Normalized(), contact.Contact.PenetrationDepth ) );
				}
			}

			float maxPenetration = 0;

			for (int i=0; i<normals.Count; i++)
			{
				for (int j=i+1; j<normals.Count; j++)
				{
					var dot = normals[i].X * normals[j].X + normals[i].Y * normals[j].Y + normals[i].Z * normals[j].Z;

					if (dot<-MinSquashDotProduct)
					{
						maxPenetration = Math.Max( maxPenetration, Math.Abs(dot) * (normals[i].W + normals[j].W) );
					}
				}
			}

			if ( maxPenetration > MaxAllowedPenetration )
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
