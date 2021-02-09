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

namespace IronStar.Editor.Systems
{
	partial class EditorModelRenderSystem : ISystem
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


		public Aspect GetAspect()
		{
			return new Aspect().Include<Transform,SFX2.RenderModel,StaticCollisionComponent>();
		}

		
		public void Add( GameState gs, Entity e )
		{
			var transform	=	e.GetComponent<Transform>();
			var model		=	e.GetComponent<SFX2.RenderModel>();

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

			for (int i=0; i<scene.Nodes.Count; i++)
			{
				var node = scene.Nodes[i];

				if (node.MeshIndex>=0 && model.AcceptCollisionNode(node))
				{
					var mesh = scene.Meshes[node.MeshIndex];
					var debugModel		=	new DebugModel( dr, mesh.Vertices.Select( v => v.Position ).ToArray(), mesh.GetIndices() );
					debugModel.World	=	transforms[i] * transform.TransformMatrix;
					debugModel.Color	=	Editor.Utils.WireColor;
					debugModel.Tag		=	e;

					dr.DebugModels.Add( debugModel );
				}
			}
		}

		
		public void Remove( GameState gs, Entity e )
		{
			dr.DebugModels.RemoveAll( dm => dm.Tag==e );
		}

		
		public void Update( GameState gs, GameTime gameTime )
		{
			Color color;
			bool selected;

			foreach ( var dm in dr.DebugModels )
			{
				var e = dm.Tag as Entity;

				if (e!=null)
				{
					editor.GetRenderProperties(e, out color, out selected );
					dm.Color	=	color;
				}
			}
			//throw new NotImplementedException();
		}
	}
}
