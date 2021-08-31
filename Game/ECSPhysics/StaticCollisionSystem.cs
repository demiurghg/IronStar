using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.SFX2;
using BEPUphysics.BroadPhaseEntries;
using Fusion.Engine.Graphics.Scenes;
using AffineTransform = BEPUutilities.AffineTransform;
using BEPUMatrix = BEPUutilities.Matrix;
using Fusion.Core.Content;

namespace IronStar.ECSPhysics
{
	class StaticCollisionSystem	: ProcessingSystem<StaticCollisionSystem.CollisionModel,StaticCollisionComponent,RenderModel,Transform>
	{
		public class CollisionModel 
		{
			public int count = 0;
			public StaticMesh[]	staticMeshes;
			public Matrix[]		transforms;
		}

		readonly CollisionModel EmptyCollisionModel = new CollisionModel() { count = 0, staticMeshes = new StaticMesh[0], transforms = new Matrix[0] };

		readonly PhysicsCore physics;

		public StaticCollisionSystem( PhysicsCore physics )
		{
			this.physics	=	physics;
		}


		protected override CollisionModel Create( Entity e, StaticCollisionComponent sc, RenderModel rm, Transform t )
		{
			if (!sc.Collidable)
			{
				return EmptyCollisionModel;
			}

			var content		=	e.gs.Content;
			
			var scene		=	string.IsNullOrWhiteSpace(rm.scenePath) ? Scene.Empty : content.Load( rm.scenePath, Scene.Empty );
			var transform	=	t.TransformMatrix;

			var cm			=	new CollisionModel();
			cm.count		=	scene.Nodes.Count;
			cm.transforms	=	new Matrix[ scene.Nodes.Count ];
			cm.staticMeshes	=	new StaticMesh[ scene.Nodes.Count ];

			scene.ComputeAbsoluteTransforms( cm.transforms );

			for ( int i=0; i<scene.Nodes.Count; i++ ) 
			{
				var node	=	scene.Nodes[i];
				int meshIdx	=	node.MeshIndex;

				if (rm.AcceptCollisionNode(node) && meshIdx>=0)
				{
					cm.staticMeshes[i]		=	CreateStaticMesh( scene.Meshes[ meshIdx ], cm.transforms[i] * transform );
					cm.staticMeshes[i].Tag	=	e;
					physics.Add( cm.staticMeshes[i] );
				}
				else
				{
					cm.staticMeshes[i]	=	null;
				}
			}

			return cm;
		}

		
		protected override void Process( Entity e, GameTime gameTime, CollisionModel cm, StaticCollisionComponent sc, RenderModel rm, Transform t )
		{
			/*var tm =  t.TransformMatrix;

			for (int i=0; i<cm.count; i++)
			{
				var lt = cm.transforms[i];
				var sm = cm.staticMeshes[i];

				if (sm!=null)
				{
					sm.WorldTransform	=	 new AffineTransform() { Matrix = MathConverter.Convert(lt * tm) };
				}
			}	*/
		}

		
		protected override void Destroy( Entity e, CollisionModel cm )
		{
			foreach ( var m in cm.staticMeshes )
			{
				if (m!=null) 
				{
					physics.Remove( m );
				}
			}
		}


		StaticMesh CreateStaticMesh( Mesh mesh, Matrix transform )
		{
			var verts	=	mesh.Vertices.Select( v => MathConverter.Convert( v.Position ) ).ToArray();
			var inds	=	mesh.GetIndices(0);

			var aft		=	new AffineTransform() { Matrix = MathConverter.Convert(transform) };

			return	new StaticMesh( verts, inds, aft );
		}
	}
}
