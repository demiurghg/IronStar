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
using System.Diagnostics;

namespace IronStar.Editor.Systems
{
	partial class EditorCharacterRenderSystem : ISystem
	{
		readonly DebugRender dr;
		readonly MapEditor editor;

		readonly BoundingBox box			=	new BoundingBox(1,1,1);
		readonly Aspect aspectTransform		=	new Aspect()
												.Include<Transform>()
												.Include<CharacterController>()
												;



		public EditorCharacterRenderSystem( MapEditor editor, DebugRender dr )
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
			if (gs.Game.RenderSystem.SkipDebugRendering) 
			{
				return;
			}

			var color		=	Color.Black;
			var selected	=	false;

			foreach ( var entity in gs.QueryEntities( aspectTransform ) )
			{
				Trace.Assert(entity!=null);

				var transform	=	entity.GetComponent<Transform>();
				var character	=	entity.GetComponent<CharacterController>();

				if (transform!=null && character!=null)
				{
					var transformMatrix = transform.TransformMatrix;

					if (editor.GetRenderProperties(entity, out color, out selected ))
					{
						var r = character.radius;
						var h = character.height;
						var p = transformMatrix.TranslationVector + Vector3.Up * h * 0.5f;
						dr.DrawCylinder( p, r, h, color, 16 );
						dr.DrawBox( box, transformMatrix, color );
						dr.DrawBasis( transformMatrix, 1, 2 ); 
					}
				}
			}
		}
	}
}
