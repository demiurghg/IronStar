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

namespace IronStar.ECSPhysics 
{
	public class StaticCollisionComponent : Component
	{
		public bool Walkable { get; set; } =  true;
		public bool Collidable { get; set; } =  true;

		/*-----------------------------------------------------------------------------------------------
		 *	Scene management operations :
		-----------------------------------------------------------------------------------------------*/

		/*Scene			scene;
		StaticMesh[]	staticMeshes;
		Matrix[]		transforms;

		void LoadScene ( GameState gs )
		{
			var content		=	gs.GetService<ContentManager>();
			var physics		=	gs.GetService<PhysicsEngineSystem>();
			
			scene			=	string.IsNullOrWhiteSpace(ScenePath) ? Scene.Empty : content.Load( ScenePath, Scene.Empty );

			transforms		=	new Matrix[ scene.Nodes.Count ];
			staticMeshes	=	new StaticMesh[ scene.Nodes.Count ];

			scene.ComputeAbsoluteTransforms( transforms );

			for ( int i=0; i<scene.Nodes.Count; i++ ) 
			{
				var node	=	scene.Nodes[i];
				int meshIdx	=	node.MeshIndex;

				if (AcceptNode(node) && meshIdx>=0)
				{
					staticMeshes[i]	=	CreateStaticMesh( scene.Meshes[ meshIdx ], transforms[i] * Transform );
					physics.Space.Add( staticMeshes[i] );
				}
				else
				{
					staticMeshes[i]	=	null;
				}
			}
		}

		
		bool AcceptNode ( Node node )
		{
			if ( string.IsNullOrWhiteSpace( CollisionFilter) ) return true;

			return node.Name.StartsWith( CollisionFilter );
		}


		StaticMesh CreateStaticMesh( Mesh mesh, Matrix transform )
		{
			var verts	=	mesh.Vertices.Select( v => MathConverter.Convert( v.Position ) ).ToArray();
			var inds	=	mesh.GetIndices(0);

			var aft		=	new AffineTransform() { Matrix = MathConverter.Convert(transform) };

			return	new StaticMesh( verts, inds, aft );
		}


		public void UnloadScene(GameState gs)
		{
			var physics		=	gs.GetService<PhysicsEngineSystem>();

			foreach ( var mesh in staticMeshes )
			{
				if (mesh!=null) 
				{	
					physics.Space.Remove( mesh );
				}
			}
		}	*/
	}
}
