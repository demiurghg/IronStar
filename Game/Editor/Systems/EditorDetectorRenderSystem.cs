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
	partial class EditorDetectorRenderSystem : ISystem
	{
		readonly DebugRender dr;
		readonly MapEditor editor;

		readonly Aspect aspectDetector		=	new Aspect().Include<Transform,DetectorComponent>();


		public EditorDetectorRenderSystem( MapEditor editor, DebugRender dr )
		{
			this.dr		=	dr;
			this.editor	=	editor;
		}


		public Aspect GetAspect()
		{
			return Aspect.Empty;
		}

		
		public void Add( IGameState gs, Entity e ) {}
		public void Remove( IGameState gs, Entity e ) {}

		
		public void Update( IGameState gs, GameTime gameTime )
		{
			if (RenderSystem.SkipDebugRendering) 
			{
				return;
			}

			var color		=	Color.Black;
			var selected	=	false;

			foreach ( var entity in gs.QueryEntities( aspectDetector ) )
			{
				var transform	=	entity.GetComponent<Transform>().TransformMatrix;
				var detector	=	entity.GetComponent<DetectorComponent>();

				if (editor.GetRenderProperties(entity, out color, out selected ))
				{
					color = selected ? color : Color.Magenta;
					dr.DrawBox( detector.LocalBounds, transform, color, 2 );
					dr.DrawBasis( transform, 1, 2 ); 
				}
			}
		}
	}
}
