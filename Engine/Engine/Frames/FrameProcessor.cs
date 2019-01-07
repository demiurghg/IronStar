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

	public partial class FrameProcessor : GameComponent {

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


		/// <summary>
		/// 
		/// </summary>
		public FrameStack Stack {
			get { return stack; }
		}


		SpriteLayer spriteLayer;
		UserTexture	baseFont;
		FrameStack stack;


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
			stack				=	new FrameStack( this );
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
		/// 
		/// </summary>
		/// <typeparam name="TFrame"></typeparam>
		/// <returns></returns>
		public TFrame QueryFrame<TFrame>() where TFrame: Frame
		{
			return (TFrame)Frame.BFSSearch( RootFrame, (frame) => frame is TFrame );
		}



		/// <summary>
		/// Push modal frame on top of stack of modal frames 
		/// and adds it to the owner frame.
		/// </summary>
		/// <param name="onwer"></param>
		/// <param name="modalFrame"></param>
		public void PushModalFrame( Frame modalFrame, Frame owner = null )
		{
			stack.PushModalFrame( modalFrame, modalFrame, owner );
		}


		public void PopModalFrame ( Frame modalFrame )
		{
			stack.PopModalFrame( modalFrame );
		}


		public void ShowDialog ( Frame dialog, Frame owner = null )
		{
			stack.PushModalFrame( dialog, dialog, owner );
			dialog.ConstrainFrame( 0 );
		}


		public void ShowDialogCentered ( Frame dialog, Frame owner = null )
		{
			stack.PushModalFrame( dialog, dialog, owner );
			dialog.CenterFrame();
		}


		public void ShowFullscreenFrame ( Frame fullscrFrame, Frame owner = null )
		{
			stack.PushModalFrame( fullscrFrame, fullscrFrame, owner );
			fullscrFrame.ExpandFrame(0);
		}


		public bool IsModalFrame ( Frame frame )
		{
			return stack.IsModalFrame( frame );
		}


		public bool IsTopLevelModalFrame ( Frame frame )
		{
			return stack.IsTopLevelModalFrame( frame );
		}


		public Frame ModalFrame {
			get { return stack.GetModalFrame(); }
		}


		public Frame TargetFrame {
			get { return stack.GetTargetFrame(); }
			set { stack.SetTargetFrame(value); }
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
