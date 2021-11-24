using System.Linq;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.Paths.PathFollowing;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using IronStar.ECS;
using BepuEntity = BEPUphysics.Entities.Entity;
using BEPUVector3 = BEPUutilities.Vector3;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using Fusion;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using System;

namespace IronStar.ECSPhysics
{
	class KinematicBody : ITransformable
	{
		public BepuEntity ConvexHull;
		public EntityMover Mover;
		public EntityRotator Rotator;
		public readonly Matrix ReCenter;
		public readonly Matrix OffCenter;

		public bool PenetrationDetected;

		public Matrix World 
		{
			get 
			{ 
				return OffCenter * MathConverter.Convert( ConvexHull.WorldTransform ); 
			}
			set 
			{
				float s;
				Vector3 p;
				Quaternion r;
				var transform = ReCenter * value;
				transform.DecomposeUniformScale( out s, out r, out p );
				Mover.TargetPosition = MathConverter.Convert( p );
				Rotator.TargetOrientation = MathConverter.Convert( r );
			}
		}

		public KinematicBody( Entity entity, Matrix transform, Node node, Mesh mesh )
		{
			var indices		=	mesh.GetIndices();
			var vertices	=	mesh.Vertices
								.Select( v2 => MathConverter.Convert( v2.Position ) )
								.ToList();

			var center		=	BEPUVector3.Zero;
			var convexShape	=	new ConvexHullShape( vertices, out center );
			ConvexHull		=	new BepuEntity( convexShape, 0 );
			ConvexHull.LocalInertiaTensorInverse  = new BEPUutilities.Matrix3x3();
			ConvexHull.Tag	=	entity;
			

			Mover			=	new EntityMover( ConvexHull );
			Mover.LinearMotor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
			Mover.LinearMotor.Settings.MaximumForce = 1000;
			Rotator			=	new EntityRotator( ConvexHull );

			ReCenter		=	Matrix.Translation( MathConverter.Convert( center ) );
			OffCenter		=	Matrix.Translation( MathConverter.Convert( -center ) );

			#warning bad matrix, pass hierarchy transform chain!
			ConvexHull.WorldTransform	=	MathConverter.Convert( ReCenter * transform );


			ConvexHull.CollisionInformation.Events.InitialCollisionDetected+=Events_InitialCollisionDetected;
			
		}


		float GetSize( BepuEntity physEntity )
		{
			var boxSize		=	physEntity.CollisionInformation.BoundingBox.Max - physEntity.CollisionInformation.BoundingBox.Min;
			var minBoxSize	=	Math.Min( Math.Min( boxSize.X, boxSize.Y ), boxSize.Z );
			return minBoxSize;
		}


		public void Update()
		{
		}

		
		private void Events_InitialCollisionDetected( EntityCollidable sender, Collidable other, CollidablePairHandler pair )
		{
			/*
			var e1 = pair?.EntityA?.Tag as Entity;
			var e2 = pair?.EntityB?.Tag as Entity;

			Log.Message("{0} tocuhes {1}", e1, e2);
			*/
		}
	}
}
