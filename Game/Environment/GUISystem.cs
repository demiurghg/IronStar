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
using Fusion;
using IronStar.Gameplay;
using IronStar.AI;
using IronStar.ECSPhysics;
using IronStar.Gameplay.Components;
using IronStar.UI.Controls;
using Fusion.Engine.Frames.Layouts;
using IronStar.SFX;

namespace IronStar.Environment
{
	public class GUISystem : ProcessingSystem<Gui, GUIComponent, Transform>
	{
		const string SOUND_IN		=	@"gui/ingame/gui_in";
		const string SOUND_OUT		=	@"gui/ingame/gui_out";
		const string SOUND_CLICK	=	@"gui/ingame/gui_click";

		readonly TriggerSystem triggerSystem;
		readonly Aspect playerAspect = new Aspect().Include<PlayerComponent,UserCommandComponent,Transform>();
		readonly PlayerInputSystem 	playerInput;
		readonly CameraSystem cameraSystem;
		readonly HashSet<Gui> activeGuis = new HashSet<Gui>(10);

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
			var fp		=	game.GetService<FrameProcessor>();
			UIState ui;

			switch (uic.UIClass)
			{
				case UIClass.SimpleButton:	ui	=	CreateSimpleButton( fp, uic.Text, entity, uic.Target );	break;
				case UIClass.DoorButton:	ui	=	CreateDoorButton  ( fp, uic.Text, entity, uic.Target );	break;
				default: throw new ArgumentException();
			}

			var gui			=	new Gui( ui );
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

								if (activeGuis.Add(resource)) 
								{
									SoundPlayback.PlaySound( entity, SOUND_IN );
								}
							}
							else
							{
								resource.UI.Mouse.FeedInGameMouseState( false, 0,0, false );
								resource.UI.ShowCursor = false;

								if (activeGuis.Remove(resource)) 
								{
									SoundPlayback.PlaySound( entity, SOUND_OUT );
								}
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


		Entity GetTargetEntity(GameState gs, string targetName)
		{
			if (string.IsNullOrWhiteSpace(targetName))
			{
				return null;
			}

			foreach ( var e in gs.QueryEntities(new Aspect().Include<TriggerComponent>()) )
			{
				var trigger = e.GetComponent<TriggerComponent>();

				if (!string.IsNullOrWhiteSpace(trigger.Name))
				{
					if (trigger.Name==targetName)
					{
						return e;
					}
				}
			}

			return null;
		}

		/*-----------------------------------------------------------------------------------------
		 *	Presets :
		-----------------------------------------------------------------------------------------*/

		UIState CreateSimpleButton( FrameProcessor fp, string text, Entity entity, string target )
		{
			var ui = new UIState( fp, true, 128,128, Color.Black );

			ui.RootFrame.Add( new Button( ui, text, 8,8,112,112, ()=> triggerSystem.Trigger(target,entity,entity) ) );

			return ui;
		}


		UIState CreateDoorButton( FrameProcessor fp, string text, Entity entity, string target )
		{
			var ui = new UIState( fp, true, 128,128, Color.Black );
			var targetEntity = GetTargetEntity( entity.gs, target );
			
			ui.RootFrame.Padding = 4;
			ui.RootFrame.Layout	=	new PageLayout()
						.Margin(4)
						.AddRow(-1,-1)
						.AddRow(70,-1)
						.AddRow(-1,-1)
						;

			var label1	=	new Label(ui,0,0,0,0,text) { TextAlignment = Alignment.MiddleCenter };
			var status	=	new Label(ui,0,0,0,0,"STATUS")  { TextAlignment = Alignment.MiddleCenter };
			
			var button	=	new Button(ui,"OPEN",0,0,0,0, ()=> 
			{ 
				SoundPlayback.PlaySound( entity, SOUND_CLICK ); 
				triggerSystem.Trigger(target,entity,entity);
			});

			button.Font =	MenuTheme.HeaderFont;
			status.Font =	MenuTheme.SmallFont;

			ui.RootFrame.Add( label1 );
			ui.RootFrame.Add( button );
			ui.RootFrame.Add( status );

			ui.RootFrame.Tick += (s,e) =>
			{
				var door		= targetEntity?.GetComponent<DoorComponent>();
				var kinematic	= targetEntity?.GetComponent<KinematicComponent>();

				if (kinematic!=null && door!=null)
				{
					switch (kinematic.State)
					{
						case KinematicState.StoppedInitial: 
							status.Text = "CLOSED"; 
							button.Text = "OPEN";
							break;
						case KinematicState.StoppedTerminal: 
							status.Text = "OPEN"; 
							button.Text = "CLOSE";
							break;
						case KinematicState.PlayForward: 
							status.Text = "OPENING"; 
							button.Text = "WAIT";
							break;
						case KinematicState.PlayBackward: 
							status.Text = "CLOSING"; 
							button.Text = "WAIT";
							break;
					}
				}
				else
				{
					status.Text = "UNKNOWN"; 
					button.Text = "DOOR\r\nOFFLINE";
					button.BackColor = Color.DarkRed;
				}
			};

			return ui;
		}
	}
}
