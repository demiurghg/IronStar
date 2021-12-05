using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Input;
using Fusion.Drivers.Graphics;
using System.Diagnostics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics;
using System.IO;
using Fusion.Core.Extensions;

namespace Fusion.Engine.Frames 
{
	public partial class FrameProcessor	: GameComponent
	{
		public int	LayerOrder	{ get; set; } = 0;

		[Config]	public bool		ShowFrames			{ get; set; }
		[Config]	public bool		SkipUserInterface	{ get; set; }
		[Config]	public bool		ShowProfilingInfo	{ get; set; }

		/// <summary>
		/// Gets FrameProcessor's sprite layer, that could be attached to RenderWorld and RenderView.
		/// </summary>
		public SpriteLayer FramesSpriteLayer {
			get {
				return spriteLayer;
			}
		}


		/// <summary>
		/// Gets image for default font
		/// </summary>
		public Texture BaseFontImage {
			get {
				return baseFont;
			}
		}


		SpriteLayer spriteLayer;
		Texture	baseFont { get { return Game.RenderSystem.Conchars; } }

		public UIState Default { get { return defaultState; } }
		UIState defaultState;

		/// <summary>
		/// Creates view
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public FrameProcessor ( Game game ) : base(game)
		{
		}



		/// <summary>
		/// Inits view
		/// </summary>
		public override void Initialize()
		{
			var rs		=	Game.GetService<RenderSystem>();

			spriteLayer	=	new SpriteLayer( rs, 1024 );
			spriteLayer.Order =	LayerOrder;
			rs.SpriteLayers.Add( spriteLayer );

			//	create root frame :
			var displayBounds	=	rs.DisplayBounds;

			defaultState		=	new UIState( this, false, displayBounds.Width, displayBounds.Height, Color.Zero );

			rs.DisplayBoundsChanged += (s,e) => UpdateDisplayBoundsChanges( defaultState.RootFrame );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void UpdateDisplayBoundsChanges ( Frame root )
		{
			root.Width	=	Game.RenderSystem.DisplayBounds.Width;
			root.Height	=	Game.RenderSystem.DisplayBounds.Height;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) 
			{
				SafeDispose( ref spriteLayer );
			}
			base.Dispose( disposing );
		}


		/// <summary>
		/// Updates stuff
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update( GameTime gameTime )
		{
			defaultState.Update( gameTime );

			spriteLayer.Order = LayerOrder;

			Draw ( gameTime, spriteLayer );
		}


		/// <summary>
		/// Draws entire interface
		/// </summary>
		/// <param name="gameTime"></param>
		void Draw ( GameTime gameTime, SpriteLayer spriteLayer )
		{
			if (SkipUserInterface) 
			{
				return;
			}

			spriteLayer.Clear();

			Frame.DrawNonRecursive( Default.RootFrame, gameTime, spriteLayer );
		}
	}
}
