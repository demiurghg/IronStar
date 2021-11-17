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

namespace IronStar.ECSPhysics
{
	public class KinematicController
	{
		class KinematicBody : ITransformable
		{
			public BepuEntity Hull;
			public EntityMover Mover;
			public EntityRotator Rotator;
			public readonly Matrix ReCenter;

			public Matrix World 
			{
				get 
				{ 
					return MathConverter.Convert( Hull.WorldTransform ); 
				}
				set 
				{
					float s;
					Vector3 p;
					Quaternion r;
					value.DecomposeUniformScale( out s, out r, out p );
					Mover.TargetPosition = MathConverter.Convert( p );
					Rotator.TargetOrientation = MathConverter.Convert( r );
				}
			}

			public KinematicBody( Node node, Mesh mesh )
			{
				var indices		=	mesh.GetIndices();
				var vertices	=	mesh.Vertices
									.Select( v2 => MathConverter.Convert( v2.Position ) )
									.ToList();

				var center			=	BEPUVector3.Zero;
				var convexShape		=	new ConvexHullShape( vertices, out center );
				var convexHull		=	new BepuEntity( convexShape, 0 );
				
				ReCenter			=	Matrix.Translation( MathConverter.Convert( center ) );
			}
		}

		int frame = 0;
		SceneView<KinematicBody> sceneView;

		
		public KinematicController( PhysicsCore physics, Scene scene, Matrix transform )
		{
			sceneView = new SceneView<KinematicBody>( scene, (n,m) => new KinematicBody(n,m), n => true );

			sceneView.ForEachMesh( 
				body => 
				{
					physics.Add( body.Hull );
					physics.Add( body.Rotator );
					physics.Add( body.Mover );
				}
			);
		}


		public void Animate( Matrix world, float fraction )
		{
			var take = sceneView.scene.Takes.FirstOrDefault();

			take.Evaluate( frame++, AnimationWrapMode.Repeat, sceneView.transforms );

			sceneView.SetTransforms( world, sceneView.transforms, false );
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
					physics.Remove( body.Hull );
					physics.Remove( body.Rotator );
					physics.Remove( body.Mover );
				}
			);
		}
	}
}
