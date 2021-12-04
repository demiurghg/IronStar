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

namespace IronStar.Environment
{
	public class UISystem : ProcessingSystem<Gui, UIComponent, Transform>
	{
		readonly TriggerSystem triggerSystem;

		public UISystem( TriggerSystem triggerSystem )
		{
			this.triggerSystem	=	triggerSystem;
		}


		protected override Gui Create( Entity entity, UIComponent uic, Transform transform )
		{
			var game	=	entity.gs.Game;
			var gui		=	new Gui();
			var ui		=	game.GetService<FrameProcessor>();

			gui.Root	=	new Frame( ui, 0,0, 640,480, "", Color.Black );
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


		protected override void Process( Entity entity, GameTime gameTime, Gui resource, UIComponent uic, Transform transform )
		{
			var game	=	entity.gs.Game;
			var ui		=	game.GetService<FrameProcessor>();

			ui.UpdateFrames( gameTime, resource.Root );

			resource.Transform = transform.TransformMatrix;
		}
	}
}
