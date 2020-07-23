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

namespace IronStar.Physics2 
{
	public class StaticCollisionModel : Component, ITransformable
	{
		public string ScenePath { get; set; } = "";
		public bool UseCollisionMesh { get; set; } = false;


		Scene			scene;
		Matrix[]		transforms;
		StaticMesh[]	collidables = null;

		public StaticCollisionModel ()
		{
		}


		public override void Added( GameState gs, Entity entity )
		{
			base.Added( gs, entity );
		}


		public override void Removed( GameState gs )
		{
			base.Removed( gs );
		}


		public void SetTransform( Matrix transform )
		{
			throw new NotImplementedException();
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Scene management operations :
		-----------------------------------------------------------------------------------------------*/

		void LoadScene ( GameState gs )
		{
			var content		=	gs.GetService<ContentManager>();
			var rs			=	gs.GetService<RenderSystem>();
			
			if (string.IsNullOrWhiteSpace(ScenePath)) 
			{
				scene	=	Scene.Empty;
			} 
			else 
			{
				scene	=	content.Load( ScenePath, Scene.Empty );
			}

			transforms	=	new Matrix[ scene.Nodes.Count ];
			scene.ComputeAbsoluteTransforms( globalTransforms );
			
			collidables		=	new StaticMesh[ scene.Nodes.Count ];

			for ( int i=0; i<scene.Nodes.Count; i++ ) 
			{
				var meshIndex = scene.Nodes[i].MeshIndex;
				
				if (meshIndex>=0) 
				{
					meshInstances[i]		= new RenderInstance( rs, scene, scene.Meshes[meshIndex] );
					meshInstances[i].Group	= InstanceGroup.Dynamic;
					meshInstances[i].Color	= Color4.Zero;
					rs.RenderWorld.Instances.Add( meshInstances[i] );
				}
				else 
				{
					meshInstances[i] = null;
				}
			}
		}


		public void UnloadScene(GameState gs)
		{
			var rs	=	gs.GetService<RenderSystem>();

			if (meshInstances!=null)
			{
				foreach ( var mesh in meshInstances )
				{
					rs.RenderWorld.Instances.Remove( mesh );
				}
			}
		}
	}
}
