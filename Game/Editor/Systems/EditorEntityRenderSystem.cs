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
	partial class EditorEntityRenderSystem : ISystem
	{
		readonly DebugRender dr;
		readonly MapEditor editor;

		readonly BoundingBox box			=	new BoundingBox(1,1,1);
		readonly Aspect aspectTransform		=	new Aspect()
												.Include<KinematicState>()
												.Exclude<DynamicBox>()
												.Exclude<SFX2.OmniLight,SFX2.SpotLight,SFX2.LightProbeSphere,SFX2.LightProbeBox>()
												.Exclude<Decal>()
												.Exclude<SFX2.RenderModel>()
												;



		public EditorEntityRenderSystem( MapEditor editor, DebugRender dr )
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
			if (gs.Game.RenderSystem.SkipDebugRendering) 
			{
				return;
			}

			var color		=	Color.Black;
			var selected	=	false;

			foreach ( var entity in gs.QueryEntities( aspectTransform ) )
			{
				var transform	=	entity.GetComponent<KinematicState>().TransformMatrix;
				var omniLight	=	entity.GetComponent<SFX2.OmniLight>();

				if (editor.GetRenderProperties(entity, out color, out selected ))
				{
					dr.DrawBox( box, transform, color );
					dr.DrawBasis( transform, 1, 2 ); 
				}
			}
		}
	}
}
