using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.ECSPhysics;
using Fusion.Engine.Graphics;

namespace IronStar.Editor.Systems
{
	partial class EditorPhysicsRenderSystem : ISystem, IDrawSystem
	{
		readonly DebugRender dr;
		readonly MapEditor editor;

		readonly Aspect aspectDynamicBody		=	new Aspect().Include<Transform,DynamicBox>();

		public EditorPhysicsRenderSystem( MapEditor editor, DebugRender dr )
		{
			this.dr		=	dr;
			this.editor	=	editor;
		}


		public Aspect GetAspect()
		{
			return Aspect.Empty;
		}

		
		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}

		public void Update( GameState gs, GameTime gameTime )
		{
		}
		
		
		public void Draw( GameState gs, GameTime gameTime )
		{
			if (gs.Game.RenderSystem.SkipDebugRendering) 
			{
				return;
			}

			var color		=	Color.Black;
			var selected	=	false;

			foreach ( var entity in gs.QueryEntities( aspectDynamicBody ) )
			{
				var transform	=	entity.GetComponent<Transform>();
				var dynamicBox	=	entity.GetComponent<DynamicBox>();

				if (editor.GetRenderProperties(entity, out color, out selected ))
				{
					var bbox = new BoundingBox( dynamicBox.Width, dynamicBox.Height, dynamicBox.Depth );
					dr.DrawBox( bbox, transform.TransformMatrix, color );
				}
			}
		}
	}
}
