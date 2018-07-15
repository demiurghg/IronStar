﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUutilities;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUVector3 = BEPUutilities.Vector3;
using BEPUTransform = BEPUutilities.AffineTransform;
using BEPUMatrix = BEPUutilities.Matrix;
using IronStar.Core;
using Fusion.Engine.Common;
using IronStar.SFX;
using Fusion.Engine.Graphics;
using Fusion.Core.Mathematics;
using Matrix = Fusion.Core.Mathematics.Matrix;
using Vector3 = Fusion.Core.Mathematics.Vector3;
using BepuEntity = BEPUphysics.Entities.Entity;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using Fusion;
using BEPUphysics.Paths.PathFollowing;
using BEPUphysics.CollisionShapes.ConvexShapes;

namespace IronStar.Physics {
	public class KinematicModel {

		readonly Matrix preTransform;
		readonly PhysicsManager physicsManager;
		readonly BepuEntity[] convexHulls;
		readonly Entity entity;
		readonly Scene scene;
		readonly int nodeCount;

		Matrix[] animSnapshot;
		EntityMover[] movers;
		EntityRotator[] rotators;
		Vector3[] offsets;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="modelManager"></param>
		/// <param name="descriptor"></param>
		/// <param name="scene"></param>
		/// <param name="entity"></param>
		public KinematicModel ( PhysicsManager physicsManager, ModelFactory descriptor, Scene scene, Entity entity )
		{
			this.entity			=	entity;
			this.physicsManager	=	physicsManager;
			this.scene			=	scene;
			this.preTransform   =   descriptor.ComputePreTransformMatrix();
			this.nodeCount		=	scene.Nodes.Count;
			
			convexHulls		=	new BepuEntity[ nodeCount ];
			var transforms	=	new Matrix[ nodeCount ];
			animSnapshot	=	new Matrix[ nodeCount ];
			offsets			=	new Vector3[ nodeCount ];

			movers			=	new EntityMover[ nodeCount ];
			rotators		=	new EntityRotator[ nodeCount ];

			scene.ComputeAbsoluteTransforms( transforms );


			for ( int i=0; i<scene.Nodes.Count; i++ ) {

				var node = scene.Nodes[i];

				if (node.MeshIndex<0) {
					continue;
				}

				var mesh		=	scene.Meshes[ node.MeshIndex ];
				var indices     =   mesh.GetIndices();
				var vertices    =   mesh.Vertices
									.Select( v2 => MathConverter.Convert( v2.Position ) )
									.ToList();

				var ms			=	new MotionState();
				var transform	=	transforms[i] * preTransform * entity.GetWorldMatrix(1);

				var p			=	transform.TranslationVector;
				var q			=   Fusion.Core.Mathematics.Quaternion.RotationMatrix( transform );

				ms.AngularVelocity	=	MathConverter.Convert( Vector3.Zero );
				ms.LinearVelocity	=	MathConverter.Convert( Vector3.Zero );
				ms.Orientation		=	MathConverter.Convert( q );
				ms.Position			=	MathConverter.Convert( p );

				//	recenter shape :
				//	https://bepuphysics.codeplex.com/wikipage?title=Shape%20Recentering
				var offset			=	BEPUVector3.Zero;
				var convexShape		=	new ConvexHullShape( vertices, out offset );
				var convexHull		=	new BepuEntity( convexShape, 0 );

				offsets[i]		=	MathConverter.Convert( offset );

				convexHull.Tag	=	entity;

				convexHulls[i] =	convexHull;

				movers[i]		=	new EntityMover( convexHull );
				rotators[i]		=	new EntityRotator( convexHull );
	
				physicsManager.PhysSpace.Add( convexHull );
				physicsManager.PhysSpace.Add( movers[i] );
				physicsManager.PhysSpace.Add( rotators[i] );
			}
		}



		public void Update ()
		{
			var dr = physicsManager.Game.RenderSystem.RenderWorld.Debug;

			//
			//	do animation stuff :
			//
			#warning ZERO ANIM FRAME!!!
			var animFrame = 0;//entity.AnimFrame;

			if (animFrame>scene.LastFrame) {
				Log.Warning("Anim frame: {0} > {1}", animFrame, scene.LastFrame);
			}
			if (animFrame<scene.FirstFrame) {
				Log.Warning("Anim frame: {0} < {1}", animFrame, scene.FirstFrame);
			}
			animFrame = MathUtil.Clamp( animFrame, scene.FirstFrame, scene.LastFrame );

			#warning USE DEFAULT TAKE!!!!
			scene.CopyLocalTransformsTo( animSnapshot );
			scene.ComputeAbsoluteTransforms( animSnapshot, animSnapshot );


			var worldMatrix = entity.GetWorldMatrix( 1 );

			for ( int i = 0; i<nodeCount; i++ ) {
				if (convexHulls[i]!=null) {

					var offset		=	Matrix.Translation( offsets[i] );

					var transform	=	offset * animSnapshot[i] * preTransform * worldMatrix;

					var p			=	transform.TranslationVector;
					var q			=   Fusion.Core.Mathematics.Quaternion.RotationMatrix( transform );

					movers[i].TargetPosition		=	MathConverter.Convert( p );
					rotators[i].TargetOrientation	=	MathConverter.Convert( q );
				}
			}
		}
		


		/// <summary>
		/// 
		/// </summary>
		public void Destroy ()
		{
			foreach ( var sm in convexHulls ) {
				if (sm!=null) {
					physicsManager.PhysSpace.Remove( sm );
				}
			}

			foreach ( var m in movers ) {
				if (m!=null) {
					physicsManager.PhysSpace.Remove( m );
				}
			}

			foreach ( var r in rotators ) {
				if (r!=null) {
					physicsManager.PhysSpace.Remove( r );
				}
			}
		}
	}
}
