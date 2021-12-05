using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics.GUI;
using IronStar.ECS;
using IronStar.Gameplay.Systems;
using Fusion.Widgets;
using Fusion;
using IronStar.Gameplay;
using IronStar.AI;
using IronStar.ECSPhysics;
using IronStar.Gameplay.Components;

namespace IronStar.Environment
{
	public class GUISystem : ProcessingSystem<Gui, GUIComponent, Transform>
	{
		readonly TriggerSystem triggerSystem;
		readonly Aspect playerAspect = new Aspect().Include<PlayerComponent,UserCommandComponent,Transform>();
		readonly PlayerInputSystem 	playerInput;
		readonly CameraSystem cameraSystem;

		public GUISystem( TriggerSystem triggerSystem, PlayerInputSystem playerInput, CameraSystem cameraSystem )
		{
			this.playerInput	=	playerInput;
			this.cameraSystem	=	cameraSystem;
			this.triggerSystem	=	triggerSystem;
		}


		protected override Gui Create( Entity entity, GUIComponent uic, Transform transform )
		{
			var game	=	entity.gs.Game;
			var ui		=	new UIState( game.GetService<FrameProcessor>(), true, 640,480, new Color(40,40,40) );
			var gui		=	new Gui( ui );

			gui.Root.Add( new Button( ui, "PUSH ME!", 10,10, 200,100, () => Log.Message("BUTTON PUSHED") ) );
			gui.Root.Add( new Button( ui, "DONT PUSH ME!", 10,120, 200,100, () => Log.Message("") ) );
			gui.Transform	=	transform.TransformMatrix;

			entity.gs.Game.RenderSystem.GuiRenderer.Guis.Add( gui );

			return gui;
		}


		protected override void Destroy( Entity entity, Gui resource )
		{
			entity.gs.Game.RenderSystem.GuiRenderer.Guis.Remove( resource );
		}


		protected override void Process( Entity entity, GameTime gameTime, Gui resource, GUIComponent uic, Transform transform )
		{
			var game	=	entity.gs.Game;

			resource.Transform = transform.TransformMatrix;

			if (uic.Interactive)
			{
				//	assume double diagonal is comfortable interactive distance :
				float engageDistance	=	resource.ComputeDiagonal() * 2.0f;

				foreach ( var playerEntity in entity.gs.QueryEntities( playerAspect ) )
				{
					var viewRay			=	cameraSystem.ViewRay;

					int x, y;

					if (resource.IsUserEngaged(viewRay, out x, out y))
					{
						resource.Root.Text = string.Format("ENGAGED : {0} {1}", x, y);

						resource.Root.Children.First().X = x;
						resource.Root.Children.First().Y = y;
					}
					else
					{
						resource.Root.Text = "-------";
					}

					/*if (distance<engageDistance)
					{
						var screenPlane		=	new Plane( transform.Position, transform.TransformMatrix.Forward );
						var invTransform	=	Matrix.Invert( transform.TransformMatrix );
						
						Vector3 hitPoint;
						
						if (userRay.Intersects(ref screenPlane, out hitPoint))
						{
							var projection	=	Vector3.TransformCoordinate( hitPoint, invTransform );

							int w = resource.Root.Width;
							int h = resource.Root.Height;
							int x = (int)( projection.X * resource.DotsPerUnit) + w / 2;
							int y = (int)(-projection.Y * resource.DotsPerUnit) + h / 2;


						}
					}*/
				}
			}

			resource.UI.Update( gameTime );
		}
	}
}
