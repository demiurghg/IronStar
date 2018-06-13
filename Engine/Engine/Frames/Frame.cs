using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Input;
using Fusion.Engine.Graphics;
using Fusion.Engine.Common;
using Forms = System.Windows.Forms;


namespace Fusion.Engine.Frames {

	public partial class Frame {

		public readonly	Game	Game;
		readonly FrameProcessor	ui;

		/// <summary>
		/// Gets frame processor instance
		/// </summary>
		public  FrameProcessor Frames {
			get { return ui; }
		}

		/// <summary>
		/// 
		/// </summary>
		public	string		Name				{ get; set; }

		/// <summary>
		/// Is frame visible. Default true.
		/// </summary>
		public	bool		Visible				{ get; set; }

		/// <summary>
		/// 
		/// </summary>
		internal bool		CanAcceptControl	{ get { return Visible && OverallColor.A != 0 && !Ghost; } }

		/// <summary>
		/// 
		/// </summary>
		internal bool		IsDrawable			{ get { return Visible && OverallColor.A != 0; } }

		/// <summary>
		/// Frame visible but does not receive input 
		/// </summary>
		public	bool		Ghost				{ get; set; }

		/// <summary>
		/// Is frame receive input. Default true.
		/// </summary>
		public	bool		Enabled				{ get; set; }

		/// <summary>
		/// Should frame fit its size to content. Default false.
		/// </summary>
		public	bool		AutoSize			{ get; set; }

		/// <summary>
		/// Tag object
		/// </summary>
		public	object		Tag;

		/// <summary>
		/// Indicated whether double click enabled on given control.
		/// Default value is False
		/// </summary>
		public  bool		IsDoubleClickEnabled { get; set; }

		/// <summary>
		/// Indicated whether manipulation enabled on given control.
		/// Default value is False
		/// </summary>
		public  bool		IsManipulationEnabled { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public	ClippingMode	 ClippingMode	{ get; set; }

		/// <summary>
		/// Overall color that used as multiplier 
		/// for all children elements
		/// </summary>
		public	Color		OverallColor		{ get; set; }

		/// <summary>
		/// Background color
		/// </summary>
		public	Color		BackColor			{ get; set; }

		/// <summary>
		/// Background color
		/// </summary>
		public	Color		BorderColor			{ get; set; }

		/// <summary>
		/// Foreground (e.g. text) color
		/// </summary>
		public	Color		ForeColor			{ get; set; }

		/// <summary>
		/// Text shadow color
		/// </summary>
		public	Color		ShadowColor			{ get; set; }

		/// <summary>
		/// Shadow offset
		/// </summary>
		public	Vector2		ShadowOffset		{ get; set; }

		public int				ImageOffsetX	{ get; set; }
		public int				ImageOffsetY	{ get; set; }
		public FrameImageMode	ImageMode		{ get; set; }
		public Color			ImageColor		{ get; set; }
		public Texture			Image			{ get; set; }
		public Rectangle		ImageDstRect	{ get; set; }
		public Rectangle		ImageSrcRect	{ get; set; }



		#region	Events
		public class KeyEventArgs : EventArgs {
			public Keys	Key = Keys.None;
			public bool Shift = false;
			public bool Ctrl = false;
			public bool Alt = false;
			public char Symbol = '\0';
			public bool Handled = false;
		}

		public class MouseEventArgs : EventArgs {
			public Keys Key = Keys.None;
			public int X = 0;
			public int Y = 0;
			public int DX = 0;
			public int DY = 0;
			public int Wheel = 0;
		}

		public class StatusEventArgs : EventArgs {
			public FrameStatus	Status;
		}

		public class MoveEventArgs : EventArgs {
			public int	X;
			public int	Y;
		}

		public class ResizeEventArgs : EventArgs {
			public int	Width;
			public int	Height;
		}

		public class TouchEventArgs : EventArgs {
			public int	 TouchID;
			public Point Location;
		}

		public class ManipulationEventArgs : EventArgs {
			public Vector2 Translation;
			public float   Scaling;
			public Vector2 DeltaTranslation;
			public float   DeltaScaling;
		}

		public event EventHandler	Tick;
		public event EventHandler	LayoutChanged;
		public event EventHandler	Activated;
		public event EventHandler	Deactivated;
		public event EventHandler	Missclick;
		public event EventHandler	Closed;
		public event EventHandler<MouseEventArgs>	MouseIn;
		public event EventHandler<MouseEventArgs>	MouseMove;
		public event EventHandler<MouseEventArgs>	MouseOut;
		public event EventHandler<MouseEventArgs>	MouseWheel;
		public event EventHandler<MouseEventArgs>	Click;
		public event EventHandler<MouseEventArgs>	DoubleClick;
		public event EventHandler<MouseEventArgs>	MouseDown;
		public event EventHandler<MouseEventArgs>	MouseUp;
		public event EventHandler<StatusEventArgs>	StatusChanged;
		public event EventHandler<MoveEventArgs>	Move;
		public event EventHandler<ResizeEventArgs>	Resize;
		public event EventHandler<TouchEventArgs>	Tap;
		public event EventHandler<TouchEventArgs>	TouchDown;
		public event EventHandler<TouchEventArgs>	TouchUp;
		public event EventHandler<TouchEventArgs>	TouchMove;
		public event EventHandler<ManipulationEventArgs>	ManipulationStart;
		public event EventHandler<ManipulationEventArgs>	ManipulationUpdate;
		public event EventHandler<ManipulationEventArgs>	ManipulationEnd;
		public event EventHandler<KeyEventArgs>				KeyDown;
		public event EventHandler<KeyEventArgs>				KeyUp;
		public event EventHandler<KeyEventArgs>				TypeWrite;
		#endregion


		/// <summary>
		/// Gets list of frame children
		/// </summary>
		public IEnumerable<Frame> Children { get { return children; } }


		/// <summary>
		/// Gets frame
		/// </summary>
		public Frame Parent { get { return parent; } }

		/// <summary>
		/// Global frame rectangle made 
		/// after all layouting and transitioning operation
		/// </summary>
		public Rectangle GlobalRectangle { get; private set; }



		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="id"></param>
		public Frame ( FrameProcessor ui )
		{
			Game	=	ui.Game;
			this.ui	=	ui;
			Init();
		}



		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="game"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="w"></param>
		/// <param name="h"></param>
		/// <param name="text"></param>
		/// <param name="backColor"></param>
		public Frame ( FrameProcessor ui, int x, int y, int w, int h, string text, SpriteFont font, Color backColor )
		{
			Game	=	ui.Game;
			this.ui	=	ui;
			Init();

			Font			=	font;
			X				=	x;
			Y				=	y;
			Width			=	w;
			Height			=	h;
			Text			=	text;
			BackColor		=	backColor;

		}


		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="game"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="w"></param>
		/// <param name="h"></param>
		/// <param name="text"></param>
		/// <param name="backColor"></param>
		public Frame ( FrameProcessor ui, int x, int y, int w, int h, string text, Color backColor )
		{
			Game	=	ui.Game;
			this.ui	=	ui;

			Init();

			Font			=	null;
			X				=	x;
			Y				=	y;
			Width			=	w;
			Height			=	h;
			Text			=	text;
			BackColor		=	backColor;

		}


		
		/// <summary>
		/// Common init 
		/// </summary>
		/// <param name="game"></param>
		void Init ()
		{
			IsManipulationEnabled	=	false;
			Padding			=	0;
			Visible			=	true;
			Enabled			=	true;
			AutoSize		=	false;
			Font			=	null;
			ForeColor		=	Color.White;
			BackColor		=	Color.Gray;
			Border			=	0;
			BorderColor		=	Color.White;
			ShadowColor		=	Color.Zero;
			OverallColor	=	Color.White;
		
			TextAlignment	=	Alignment.TopLeft;

			Anchor			=	FrameAnchor.Left | FrameAnchor.Top;

			ImageColor		=	Color.White;
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Hierarchy stuff
		 * 
		-----------------------------------------------------------------------------------------*/

		private	List<Frame>	children	=	new List<Frame>();
		private Frame		parent		=	null;
		

		/// <summary>
		/// Adds frame
		/// </summary>
		/// <param name="frame"></param>
		public virtual void Add ( Frame frame )
		{
			if ( !children.Contains(frame) ) {
				children.Add( frame );
				frame.parent	=	this;
				frame.OnStatusChanged( FrameStatus.None );
				layoutDirty = true;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="frame"></param>
		public virtual void Clear ()
		{
			var toRemove = children.ToArray();

			foreach ( var child in toRemove ) {
				child.Close();
			}

			layoutDirty = true;
		}


		/// <summary>
		/// Inserts frame at specified index
		/// </summary>
		/// <param name="index"></param>
		/// <param name="frame"></param>
		public virtual void Insert ( int index, Frame frame )
		{
			if ( !children.Contains(frame) ) {
				children.Insert( index, frame );
				frame.parent	=	this;
				frame.OnStatusChanged( FrameStatus.None );
				layoutDirty = true;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="frame"></param>
		public virtual void Remove ( Frame frame )
		{
			if ( children.Contains(frame) ) {

				children.Remove( frame );
				frame.parent	=	null;
				layoutDirty = true;

				ui.WipeRefs(frame);

				Closed?.Invoke(this, EventArgs.Empty);
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public virtual void Close ()
		{
			Parent?.Remove(this);
		}


		/// <summary>
		/// Sorts child frame (Unstable!)
		/// </summary>
		/// <param name="comparison"></param>
		public void SortChildren ( Comparison<Frame> comparison )
		{
			children.Sort(comparison);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public List<Frame>	GetAncestorList ()
		{
			var list = new List<Frame>();

			var frame = this;

			while ( frame != null ) {
				list.Add( frame );
				frame = frame.parent;
			}

			return list;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="action"></param>
		public void ForEachAncestor ( Action<Frame> action ) 
		{
			GetAncestorList().ForEach( f => action(f) );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="action"></param>
		public void ForEachChildren ( Action<Frame> action ) 
		{
			children.ForEach( f => action(f) );
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Input stuff :
		 * 
		-----------------------------------------------------------------------------------------*/

		internal void OnStatusChanged ( FrameStatus status )
		{
			StatusChanged?.Invoke( this, new StatusEventArgs() { Status = status } );
		}


		internal void OnClick ( Point location, Keys key, bool doubleClick)
		{
			int x = location.X - GlobalRectangle.X;
			int y = location.Y - GlobalRectangle.Y;

			if (doubleClick) {
				DoubleClick?.Invoke( this, new MouseEventArgs() { Key = key, X = x, Y = y } );
			} else {
				Click?.Invoke( this, new MouseEventArgs() { Key = key, X = x, Y = y } );
			}
		}


		internal void OnMouseIn ( Point location )
		{
			int x = location.X - GlobalRectangle.X;
			int y = location.Y - GlobalRectangle.Y;

			MouseIn?.Invoke( this, new MouseEventArgs() { Key = Keys.None, X = x, Y = y } );
		}


		internal void OnMouseMove ( Point location, int dx, int dy)
		{
			int x = location.X - GlobalRectangle.X;
			int y = location.Y - GlobalRectangle.Y;

			MouseMove?.Invoke( this, new MouseEventArgs() { Key = Keys.None, X = x, Y = y, DX = dx, DY = dy } );
		}


		internal void OnMouseOut ( Point location )
		{
			int x = location.X - GlobalRectangle.X;
			int y = location.Y - GlobalRectangle.Y;

			MouseOut?.Invoke( this, new MouseEventArgs() { Key = Keys.None, X = x, Y = y } );
		}


		internal void OnMouseDown ( Point location, Keys key )
		{
			int x = location.X - GlobalRectangle.X;
			int y = location.Y - GlobalRectangle.Y;

			MouseDown?.Invoke( this, new MouseEventArgs() { Key = key, X = x, Y = y } );
		}


		internal void OnMouseUp ( Point location, Keys key )
		{
			int x = location.X - GlobalRectangle.X;
			int y = location.Y - GlobalRectangle.Y;

			MouseUp?.Invoke( this, new MouseEventArgs() { Key = key, X = x, Y = y } );
		}


		internal void OnMouseWheel ( int wheel )
		{
			if (MouseWheel!=null) {
				MouseWheel( this, new MouseEventArgs(){ Wheel = wheel } );
			} else if ( Parent!=null ) {
				Parent.OnMouseWheel( wheel );
			}
		}


		internal void OnTap ( int id, Point location )
		{
			Tap?.Invoke( this, new TouchEventArgs() { TouchID = id, Location = location } );
		}


		internal void OnTouchDown ( int id, Point location )
		{
			TouchDown?.Invoke( this, new TouchEventArgs() { TouchID = id, Location = location } );
		}


		internal void OnTouchUp ( int id, Point location )
		{
			TouchUp?.Invoke( this, new TouchEventArgs() { TouchID = id, Location = location } );
		}


		internal void OnTouchMove ( int id, Point location )
		{
			TouchMove?.Invoke( this, new TouchEventArgs() { TouchID = id, Location = location } );
		}


		internal void OnManipulationStart ( Vector2 translation, float scaling, Vector2 deltaTranslation, float deltaScaling )
		{
			ManipulationStart?.Invoke( this, new ManipulationEventArgs() {
				Translation = translation,
				Scaling = scaling,
				DeltaTranslation = deltaTranslation,
				DeltaScaling = deltaScaling,
			} );
		}

		internal void OnManipulationUpdate ( Vector2 translation, float scaling, Vector2 deltaTranslation, float deltaScaling )
		{
			ManipulationUpdate?.Invoke( this, new ManipulationEventArgs() {
				Translation = translation,
				Scaling = scaling,
				DeltaTranslation = deltaTranslation,
				DeltaScaling = deltaScaling,
			} );
		}

		internal void OnManipulationEnd ( Vector2 translation, float scaling, Vector2 deltaTranslation, float deltaScaling )
		{
			ManipulationEnd?.Invoke( this, new ManipulationEventArgs() {
				Translation = translation,
				Scaling = scaling,
				DeltaTranslation = deltaTranslation,
				DeltaScaling = deltaScaling,
			} );
		}


		internal void OnTick ()
		{
			Tick?.Invoke( this, EventArgs.Empty );
		}

		internal void OnActivate ()
		{
			Activated?.Invoke( this, EventArgs.Empty );
		}

		internal void OnDeactivate ()
		{
			Deactivated?.Invoke( this, EventArgs.Empty );
		}

		internal void OnKeyDown( Keys key, bool shift, bool alt, bool ctrl )
		{
			var eventArgs = new KeyEventArgs() { Key = key, Shift = shift, Alt = alt, Ctrl = ctrl };
			var frame = this;

			while (frame!=null && !eventArgs.Handled) {
				frame.KeyDown?.Invoke( this, eventArgs );
				frame = frame.Parent;
			}
		}

		internal void OnKeyUp( Keys key, bool shift, bool alt, bool ctrl )
		{
			var eventArgs = new KeyEventArgs() { Key = key, Shift = shift, Alt = alt, Ctrl = ctrl };
			var frame = this;

			while (frame!=null && !eventArgs.Handled) {
				frame.KeyUp?.Invoke( this, eventArgs );
				frame = frame.Parent;
			}
		}

		internal void OnTypeWrite( Keys key, char symbol, bool shift, bool alt, bool ctrl )
		{
			var eventArgs = new KeyEventArgs() { Key = key, Symbol = symbol, Shift = shift, Alt = alt, Ctrl = ctrl };
			var frame = this;

			while (frame!=null && !eventArgs.Handled) {
				frame.TypeWrite?.Invoke( this, eventArgs );
				frame = frame.Parent;
			}
		}


		internal void OnMissclick ()
		{
			Missclick?.Invoke( this, EventArgs.Empty );
		}

		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Update and draw stuff :
		 * 
		-----------------------------------------------------------------------------------------*/

		public static List<Frame> BFSList ( Frame v )
		{
			Queue<Frame> Q = new Queue<Frame>();
			List<Frame> list = new List<Frame>();

			Q.Enqueue( v );

			while ( Q.Any() ) {
				
				var t = Q.Dequeue();
				list.Add( t );

				foreach ( var u in t.Children ) {
					Q.Enqueue( u );
				}
			}

			return list;
		}
			

		void UpdateGlobalRect ( int px, int py ) 
		{
			GlobalRectangle = new Rectangle( X + px, Y + py, Width, Height );
			ForEachChildren( ch => ch.UpdateGlobalRect( px + X, py + Y ) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="parentX"></param>
		/// <param name="parentY"></param>
		/// <param name="frame"></param>
		internal void UpdateInternalNonRecursive ( GameTime gameTime )
		{
			var bfsList  = BFSList( this );
			var bfsListR = bfsList.ToList();
			bfsListR.Reverse();

			//	run layout engine twice to handle back propagation
			bfsList.ForEach( f => f.RunLayoutInternal() );
			bfsList.ForEach( f => f.RunLayoutInternal() );
			bfsList.ForEach( f => f.RunLayoutInternal() );

			bfsList.ForEach( f => f.UpdateMove() );
			bfsList.ForEach( f => f.UpdateResize() );

			UpdateGlobalRect(0,0);

			bfsList .ForEach( f => f.OnTick() );
			bfsList .ForEach( f => f.Update( gameTime ) );
		}



		class DrawFrameItem {
			public DrawFrameItem ( Frame frame, Color color, Rectangle outerClip, Rectangle innerClip, string text )
			{
				this.Frame		=	frame;
				this.OuterClip	=	outerClip;
				this.InnerClip	=	innerClip;
				this.Color		=	color;
				this.Text		=	text;
			}
			public Frame Frame;
			public Color Color;
			public Rectangle OuterClip;
			public Rectangle InnerClip;
			public string Text;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="sb"></param>
		static internal void DrawNonRecursive ( Frame rootFrame, GameTime gameTime, SpriteLayer spriteLayer )
		{
			if (rootFrame==null) {
				return;
			}

			var stack = new Stack<DrawFrameItem>();
			var list  = new List<DrawFrameItem>();

			stack.Push( new DrawFrameItem(rootFrame, Color.White, rootFrame.GlobalRectangle, rootFrame.GetBorderedRectangle(), rootFrame.Text ) );


			while (stack.Any()) {
				
				var currentDrawFrame = stack.Pop();

				if (!currentDrawFrame.Frame.IsDrawable) {
					continue;
				}

				list.Add( currentDrawFrame );

				foreach ( var child in currentDrawFrame.Frame.Children.Reverse() ) {

					var color = currentDrawFrame.Color * child.OverallColor;
					var inner = Clip( child.GetBorderedRectangle(), currentDrawFrame.InnerClip );
					var outer = Clip( child.GlobalRectangle,		currentDrawFrame.InnerClip );

					if ( MathUtil.IsRectInsideRect( child.GlobalRectangle, currentDrawFrame.InnerClip ) ) {
						stack.Push( new DrawFrameItem(child, color, outer, inner, currentDrawFrame.Text + "-" + child.Text ) );
					}
				}
			}



			for (int i=0; i<list.Count; i++) {
				var drawFrame = list[i];

				spriteLayer.SetClipRectangle( i*2+0, drawFrame.OuterClip, drawFrame.Color );
				spriteLayer.SetClipRectangle( i*2+1, drawFrame.InnerClip, drawFrame.Color );

				drawFrame.Frame.DrawFrameBorders( spriteLayer, i*2+0 );
				drawFrame.Frame.DrawFrame( gameTime, spriteLayer,   i*2+1 );
			}
		}



		/// <summary>
		/// Clips one rectangle by another.
		/// </summary>
		/// <param name="child"></param>
		/// <param name="parent"></param>
		/// <returns></returns>
		static Rectangle Clip ( Rectangle child, Rectangle parent )
		{
			var r = new Rectangle();

			r.Left		=	Math.Max( child.Left,	parent.Left		);
			r.Right		=	Math.Min( child.Right,	parent.Right	);
			r.Top		=	Math.Max( child.Top,	parent.Top		);
			r.Bottom	=	Math.Min( child.Bottom,	parent.Bottom	);

			return r;
		}



		/// <summary>
		/// Updates frame stuff.
		/// </summary>
		/// <param name="gameTime"></param>
		protected virtual void Update ( GameTime gameTime )
		{
		}



		/// <summary>
		/// Draws frame stuff
		/// </summary>
		void DrawFrameBorders ( SpriteLayer spriteLayer, int clipRectIndex )
		{
			int gx	=	GlobalRectangle.X;
			int gy	=	GlobalRectangle.Y;
			int w	=	Width;
			int h	=	Height;
			int bt	=	BorderTop;
			int bb	=	BorderBottom;
			int br	=	BorderRight;
			int bl	=	BorderLeft;

			var whiteTex = Game.RenderSystem.WhiteTexture;

			var clr	=	BorderColor;

			spriteLayer.Draw( whiteTex,	gx,				gy,				w,		bt,				clr, clipRectIndex ); 
			spriteLayer.Draw( whiteTex,	gx,				gy + h - bb,	w,		bb,				clr, clipRectIndex ); 
			spriteLayer.Draw( whiteTex,	gx,				gy + bt,		bl,		h - bt - bb,	clr, clipRectIndex ); 
			spriteLayer.Draw( whiteTex,	gx + w - br,	gy + bt,		br,		h - bt - bb,	clr, clipRectIndex ); 

			spriteLayer.Draw( whiteTex,	GetBorderedRectangle(), BackColor, clipRectIndex );
		}



		/// <summary>
		/// Draws frame stuff.
		/// </summary>
		/// <param name="gameTime"></param>
		protected virtual void DrawFrame ( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			DrawFrameImage( spriteLayer, clipRectIndex );
			DrawFrameText ( spriteLayer, clipRectIndex );
		}



		/// <summary>
		/// Adjusts frame size to content, text, image etc.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="?"></param>
		protected virtual void Adjust ()
		{
			throw new NotImplementedException();
		}



		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Utils :
		 * 
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// Checks move and resize and calls appropriate events
		/// </summary>
		protected void UpdateMove ()
		{
			if (moveDirty) {
				Move?.Invoke( this, new MoveEventArgs() { X = this.X, Y = this.Y } );
				moveDirty = false;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		protected void UpdateResize ()
		{
			if (sizeDirty) {

				Resize?.Invoke( this, new ResizeEventArgs(){ Width = Width, Height = Height } );

				ForEachChildren( f => f.UpdateAnchors( oldWidth, oldHeight, Width, Height ) );

				oldWidth	=	Width;
				oldHeight	=	Height;

				sizeDirty	=	false;
			}

		}


		/// <summary>
		/// Get global rectangle bound by borders
		/// </summary>
		/// <returns></returns>
		public Rectangle GetBorderedRectangle ()
		{
			return new Rectangle( 
				GlobalRectangle.X + BorderLeft, 
				GlobalRectangle.Y + BorderTop, 
				Width - BorderLeft - BorderRight,
				Height - BorderTop - BorderBottom );
		}



		/// <summary>
		/// Get global rectangle padded and bound by borders
		/// </summary>
		/// <returns></returns>
		public Rectangle GetPaddedRectangle ( bool global = true )
		{
			int x = global ? GlobalRectangle.X : 0;
			int y = global ? GlobalRectangle.Y : 0;

			return new Rectangle( 
				x + BorderLeft + PaddingLeft, 
				y + BorderTop + PaddingTop, 
				Width  - BorderLeft - BorderRight - PaddingLeft - PaddingRight,
				Height - BorderTop - BorderBottom - PaddingTop - PaddingBottom );
		}


		
		/// <summary>
		/// 
		/// </summary>
		protected virtual void DrawFrameImage (SpriteLayer spriteLayer, int clipRectIndex )
		{
			if (Image==null) {
				return;
			}

			var gp = GetPaddedRectangle();
			var bp = GetBorderedRectangle();
			var gr = GlobalRectangle;

			if (ImageMode==FrameImageMode.Stretched) {
				spriteLayer.Draw( Image, bp, ImageColor, clipRectIndex );
				return;
			}

			if (ImageMode==FrameImageMode.Centered) {
				int x = bp.X + gp.Width/2  - Image.Width/2	+ ImageOffsetX;
				int y = bp.Y + gp.Height/2 - Image.Height/2	+ ImageOffsetY;
				spriteLayer.Draw( Image, x, y, Image.Width, Image.Height, ImageColor, clipRectIndex );
				return;
			}

			if (ImageMode==FrameImageMode.Tiled) {
				spriteLayer.Draw( Image, bp, new Rectangle(0,0,bp.Width,bp.Height), ImageColor, clipRectIndex );
				return;
			}

			if (ImageMode == FrameImageMode.DirectMapped) {
				spriteLayer.Draw(Image, bp, bp, ImageColor, clipRectIndex );
				return;
			}

			if (ImageMode == FrameImageMode.Manual) {
				var dstRect = ImageDstRect;
				var srcRect = ImageSrcRect;
				dstRect.X += gr.X;
				dstRect.Y += gr.Y;
				spriteLayer.Draw( Image, dstRect, srcRect, ImageColor, clipRectIndex );
			}

		}

	}
}

