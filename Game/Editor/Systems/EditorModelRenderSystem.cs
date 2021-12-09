using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using Fusion.Engine.Graphics;
using IronStar.ECSPhysics;
using Fusion.Core.Content;
using Fusion.Engine.Graphics.Scenes;
using IronStar.SFX2;
using BEPUutilities.Threading;

namespace IronStar.Editor.Systems
{
	partial class EditorModelRenderSystem : ProcessingSystem<DebugModel[], Transform, RenderModel, StaticCollisionComponent>
	{
		readonly DebugRender dr;
		readonly MapEditor editor;
		readonly ContentManager content;


		public EditorModelRenderSystem( MapEditor editor, DebugRender dr )
		{
			this.dr			=	dr;
			this.editor		=	editor;
			this.content	=	editor.Content;
		}

		
		protected override DebugModel[] Create( Entity entity, Transform component1, RenderModel component2, StaticCollisionComponent component3 )
		{
			var transform	=	entity.GetComponent<Transform>();
			var model		=	entity.GetComponent<RenderModel>();

			Scene scene;

			if (string.IsNullOrWhiteSpace(model.scenePath)) 
			{
				scene = Scene.CreateEmptyScene();
			}
			else
			{
				scene = content.Load( model.scenePath, Scene.Empty );
			}
			
			var transforms	=	scene.ComputeAbsoluteTransforms();
			var modelList	=	new List<DebugModel>();

			for (int i=0; i<scene.Nodes.Count; i++)
			{
				var node = scene.Nodes[i];

				if (node.MeshIndex>=0 && model.AcceptCollisionNode(node))
				{
					var mesh = scene.Meshes[node.MeshIndex];
					var debugModel		=	new DebugModel( dr, mesh.Vertices.Select( v => v.Position ).ToArray(), mesh.GetIndices() );
					debugModel.World	=	transforms[i] * transform.TransformMatrix;
					debugModel.Color	=	Editor.Utils.WireColor;
					debugModel.Tag		=	entity;

					modelList.Add( debugModel );
					dr.DebugModels.Add( debugModel );
				}
			}

			return modelList.ToArray();
		}

		
		protected override void Destroy( Entity entity, DebugModel[] models )
		{
			foreach (var m in models)
			{
				dr.DebugModels.Remove( m );
				m?.Dispose();
			}
		}

		
		protected override void Process( Entity entity, GameTime gameTime, DebugModel[] models, Transform component1, RenderModel component2, StaticCollisionComponent component3 )
		{
			Color color;
			bool selected;

			foreach ( var dm in models )
			{
				editor.GetRenderProperties(entity, out color, out selected );
				dm.Color	=	color;
			}
		}
	}
}
