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
using BEPUphysics;
using BepuEntity = BEPUphysics.Entities.Entity;
using BEPUphysics.Character;
using BEPUCharacterController = BEPUphysics.Character.CharacterController;
using Fusion.Core.IniParser.Model;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.PositionUpdating;
using BEPUphysics.CollisionRuleManagement;
using IronStar.ECS;
using Fusion.Engine.Graphics.Scenes;
using AffineTransform = BEPUutilities.AffineTransform;
using BEPUphysics.Paths.PathFollowing;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUVector3 = BEPUutilities.Vector3;
using BEPUTransform = BEPUutilities.AffineTransform;
using BEPUMatrix = BEPUutilities.Matrix;
using BEPUphysics.Constraints.SingleEntity;

namespace IronStar.ECSPhysics
{
	public class KinematicController
	{
		class KinematicBody : ITransformable
		{
			public BepuEntity ConvexHull;
			public EntityMover Mover;
			public EntityRotator Rotator;
			public readonly Matrix ReCenter;
			public readonly Matrix OffCenter;

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

			public KinematicBody( Entity entity, Node node, Mesh mesh )
			{
				var indices		=	mesh.GetIndices();
				var vertices	=	mesh.Vertices
									.Select( v2 => MathConverter.Convert( v2.Position ) )
									.ToList();

				var center		=	BEPUVector3.Zero;
				var convexShape	=	new ConvexHullShape( vertices, out center );
				ConvexHull		=	new BepuEntity( convexShape, 0 );
				ConvexHull.Tag	=	entity;
				Mover			=	new EntityMover( ConvexHull );
				Rotator			=	new EntityRotator( ConvexHull );

				ReCenter		=	Matrix.Translation( MathConverter.Convert( center ) );
				OffCenter		=	Matrix.Translation( MathConverter.Convert( -center ) );
			}
		}

		readonly SceneView<KinematicBody> sceneView;
		readonly AnimationKey[] frame0;
		readonly AnimationKey[] frame1;


		
		public KinematicController( PhysicsCore physics, Entity entity, Scene scene, Matrix transform )
		{
			sceneView = new SceneView<KinematicBody>( scene, (n,m) => new KinematicBody(entity,n,m), n => true );

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
	}
}
