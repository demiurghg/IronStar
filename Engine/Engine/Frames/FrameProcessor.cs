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

namespace Fusion.Engine.Frames {


	public class FrameProcessor : GameComponent {

		public int	LayerOrder	{ get; set; } = 0;

		[Config]	public bool		ShowFrames			{ get; set; }
		[Config]	public bool		SkipUserInterface	{ get; set; }
		[Config]	public bool		ShowProfilingInfo	{ get; set; }


		public Point MousePosition { get; internal set; }


		/// <summary>
		/// Sets and gets current root frame.
		/// </summary>
		public	Frame RootFrame { get; private set; }


		/// <summary>
		/// Gets ans sets default font.
		/// If this value not set, 
		/// the creation of Frames without explicitly specified font will fail.
		/// </summary>
		public	SpriteFont DefaultFont { get; set; }


		/// <summary>
		/// Gets and sets modal frame for entire UI.
		/// This property does not set TargetFrame.
		/// </summary>
		public Frame ModalFrame {
			get; set;
		} = null;


		/// <summary>
		/// Gets and sets current target frame.
		/// </summary>
		public	Frame TargetFrame { 
			get { 
				return targetFrame;
			}
			set {
				if (targetFrame!=value) {
					targetFrame?.OnDeactivate();
					value?.OnActivate();
					targetFrame = value;
				}
			}
		}
		Frame targetFrame = null;


		MouseProcessor		mouseProcessor;
		TouchProcessor		touchProcessor;
		KeyboardProcessor	keyboardProcessor;


		/// <summary>
		/// Gets keyboard processor 
		/// </summary>
		public KeyboardProcessor Keyboard {
			get {
				return keyboardProcessor;
			}
		}


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
		public UserTexture BaseFontImage {
			get {
				return baseFont;
			}
		}

		SpriteLayer spriteLayer;
		UserTexture	baseFont;


		/// <summary>
		/// Creates view
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public FrameProcessor ( Game game ) : base(game)
		{
			mouseProcessor		=	new MouseProcessor( Game, this );
			touchProcessor		=	new TouchProcessor( Game, this );
			keyboardProcessor	=	new KeyboardProcessor( Game, this );
		}



		/// <summary>
		/// Inits view
		/// </summary>
		public override void Initialize()
		{
			using ( var ms = new MemoryStream( Properties.Resources.conchars ) ) {
				baseFont = UserTexture.CreateFromTga( Game.RenderSystem, ms, false );
			}

			var rs		=	Game.GetService<RenderSystem>();

			spriteLayer	=	new SpriteLayer( rs, 1024 );
			spriteLayer.Order =	LayerOrder;

			rs.SpriteLayers.Add( spriteLayer );

			//	create root frame :
			var vp		=	rs.DisplayBounds;
			RootFrame	=	new Frame( this, 0,0, vp.Width, vp.Height, "", null, Color.Zero );
			rs.DisplayBoundsChanged += RenderSystem_DisplayBoundsChanged;

			mouseProcessor.Initialize();
			touchProcessor.Initialize();
			keyboardProcessor.Initialize();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void RenderSystem_DisplayBoundsChanged ( object sender, EventArgs e )
		{
			RootFrame.Width		=	Game.RenderSystem.DisplayBounds.Width;
			RootFrame.Height	=	Game.RenderSystem.DisplayBounds.Height;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref spriteLayer );
				SafeDispose( ref baseFont );
			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// Resets 
		/// </summary>
		public void Reset ()
		{
			RootFrame.Clear();
			TargetFrame = null;
			ModalFrame = null;
		}



		/// <summary>
		/// Wipes possible refs to given frame or its children (recursivly)
		/// </summary>
		/// <param name="frame"></param>
		public void WipeRefs ( Frame frame )
		{
			var scan = ModalFrame;

			while (scan!=null) {
				if (scan==frame) {
					ModalFrame = null;
					break;
				} else {
					scan = scan.Parent;
				}
			}

			//---------------

			scan = TargetFrame;	

			while (scan!=null) {
				if (scan==frame) {
					TargetFrame = null;
					break;
				} else {
					scan = scan.Parent;
				}
			}
		}


		/// <summary>
		/// Updates stuff
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update( GameTime gameTime )
		{
			var viewCtxt	=	new ViewContext();

			touchProcessor.UpdateManipulations( gameTime );
			keyboardProcessor.UpdateKeyboard( gameTime );

			spriteLayer.Order = LayerOrder;

			//
			//	Update and profile UI stuff :
			//
			Stopwatch sw = new Stopwatch();
			sw.Start();

				RootFrame?.UpdateInternalNonRecursive( gameTime );

			sw.Stop();

			//
			//	Draw UI :
			//
			Draw ( gameTime, spriteLayer );
		}



		/// <summary>
		/// Draws entire interface
		/// </summary>
		/// <param name="gameTime"></param>
		void Draw ( GameTime gameTime, SpriteLayer spriteLayer )
		{
			if (SkipUserInterface) {
				return;
			}

			spriteLayer.Clear();

			Frame.DrawNonRecursive( RootFrame, gameTime, spriteLayer );
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Input processing :
		 * 
		-----------------------------------------------------------------------------------------*/


		/// <summary>
		/// 
		/// </summary>
		/// <param name="root"></param>
		/// <returns></returns>
		public Frame GetHoveredFrame ( Point location )
		{
			Frame mouseHoverFrame = null;

			var startFrame = ModalFrame ?? RootFrame;

			UpdateHoverRecursive( startFrame, location, ref mouseHoverFrame );

			return mouseHoverFrame;
		}



		/// <summary>
		/// Updates current hovered frame
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="viewCtxt"></param>
		public void UpdateHoverRecursive ( Frame frame, Point p, ref Frame mouseHoverFrame )
		{
			if (frame==null) {
				return;
			}

			var absLeft		=	frame.GlobalRectangle.Left;
			var absTop		=	frame.GlobalRectangle.Top;
			var absRight	=	frame.GlobalRectangle.Right;
			var absBottom	=	frame.GlobalRectangle.Bottom;

			if (!frame.CanAcceptControl) {
				return;
			}
			
			bool hovered	=	p.X >= absLeft 
							&&	p.X <  absRight 
							&&	p.Y >= absTop
							&&	p.Y <  absBottom;

			if (hovered) {
				mouseHoverFrame = frame;
				foreach (var child in frame.Children) {
					UpdateHoverRecursive( child, p, ref mouseHoverFrame );
				}
			}

		}
	}
}
