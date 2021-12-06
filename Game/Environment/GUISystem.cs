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
using Fusion.Widgets.Dialogs;
using Fusion.Widgets.Binding;

namespace IronStar.Environment
{
	public class GUISystem : ProcessingSystem<Gui, GUIComponent, Transform>
	{
		readonly TriggerSystem triggerSystem;
		readonly Aspect playerAspect = new Aspect().Include<PlayerComponent,UserCommandComponent,Transform>();
		readonly PlayerInputSystem 	playerInput;
		readonly CameraSystem cameraSystem;

		/// <summary>
		/// Indicates that player is interacting with in-game GUI
		/// </summary>
		public bool Engaged { get; private set; }

		public GUISystem( TriggerSystem triggerSystem, PlayerInputSystem playerInput, CameraSystem cameraSystem )
		{
			this.playerInput	=	playerInput;
			this.cameraSystem	=	cameraSystem;
			this.triggerSystem	=	triggerSystem;
		}


		protected override Gui Create( Entity entity, GUIComponent uic, Transform transform )
		{
			var game	=	entity.gs.Game;
			var ui		=	new UIState( game.GetService<FrameProcessor>(), true, 640,480, Color.Black );
			var gui		=	new Gui( ui );

			gui.Root.Add( new Button( ui, "PUSH ME!", 10,10, 200,100, () => Log.Message("BUTTON PUSHED") ) );
			gui.Root.Add( new Button( ui, "DONT PUSH ME!", 10,120, 200,100, () => Log.Message("----") ) );

			ColorPicker.ShowDialog( ui, 100, 100, new PropertyBinding( gui.Root, typeof(Frame).GetProperty("BackColor") ) );

			gui.Transform	=	transform.TransformMatrix;

			entity.gs.Game.RenderSystem.GuiRenderer.Guis.Add( gui );

			return gui;
		}


		protected override void Destroy( Entity entity, Gui resource )
		{
			entity.gs.Game.RenderSystem.GuiRenderer.Guis.Remove( resource );
		}


		public override void Update( IGameState gs, GameTime gameTime )
		{
			var players = gs.QueryEntities( playerAspect );
			Engaged = false;
			
			foreach ( var player in players )
			{
				var ucc			=	player.GetComponent<UserCommandComponent>();
				var viewRay		=	cameraSystem.ViewRay;

				ForEach( gs, gameTime, (entity, gt, resource, uic, transform) =>
				{
					if (uic.Interactive)
					{
						//	assume double diagonal is comfortable interactive distance :
						float engageDistance	=	resource.ComputeDiagonal() * 2.0f;

						foreach ( var playerEntity in entity.gs.QueryEntities( playerAspect ) )
						{
							int x, y;

							if (resource.IsUserEngaged(viewRay, out x, out y))
							{
								var button = playerInput.LastCommand.Action.HasFlag(UserAction.PushGUI);

								Engaged |= true;
								resource.UI.Mouse.FeedInGameMouseState( true, x,y, button );
								resource.UI.ShowCursor = true;
							}
							else
							{
								resource.UI.Mouse.FeedInGameMouseState( false, 0,0, false );
								resource.UI.ShowCursor = false;
							}
						}
					}
					else
					{
						resource.UI.ShowCursor = false;
					}
				});
			}

			base.Update( gs, gameTime );
		}


		protected override void Process( Entity entity, GameTime gameTime, Gui resource, GUIComponent uic, Transform transform )
		{
			var game	=	entity.gs.Game;

			resource.Transform = transform.TransformMatrix;

			resource.UI.Update( gameTime );
		}
	}
}
