﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Input;
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
		/// Text font
		/// </summary>
		public	SpriteFont	Font				{ get; set; }

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
		/// Indicated whether double click enabled on given control.
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


		/// <summary>
		/// Local X position of the frame
		/// </summary>
		public	int			X					{ get; set; }

		/// <summary>
		/// Local Y position of the frame
		/// </summary>
		public	int			Y					{ get; set; }

		/// <summary>
		///	Width of the frame
		/// </summary>
		public virtual int	Width				{ get; set; }

		/// <summary>
		///	Height of the frame
		/// </summary>
		public virtual int	Height				{ get; set; }

		/// <summary>
		/// Left gap between frame and its content
		/// </summary>
		public	int			PaddingLeft			{ get; set; }

		/// <summary>
		/// Right gap between frame and its content
		/// </summary>
		public	int			PaddingRight		{ get; set; }

		/// <summary>
		/// Top gap  between frame and its content
		/// </summary>
		public	int			PaddingTop			{ get; set; }

		/// <summary>
		/// Bottom gap  between frame and its content
		/// </summary>
		public	int			PaddingBottom		{ get; set; }

		/// <summary>
		/// Top and bottom padding
		/// </summary>
		public	int			VPadding			{ set { PaddingBottom = PaddingTop = value; } }

		/// <summary>
		///	Left and right padding
		/// </summary>
		public	int			HPadding			{ set { PaddingLeft = PaddingRight = value; } }

		/// <summary>
		/// Top, bottom, left and right padding
		/// </summary>
		public	int			Padding				{ set { VPadding = HPadding = value; } }

		/// <summary>
		/// 
		/// </summary>
		public	int			BorderTop			{ get; set; }

		/// <summary>
		/// 
		/// </summary>
		public	int			BorderBottom		{ get; set; }

		/// <summary>
		/// 
		/// </summary>
		public	int			BorderLeft			{ get; set; }

		/// <summary>
		/// 
		/// </summary>
		public	int			BorderRight			{ get; set; }


		/// <summary>
		/// Top, bottom, left and right margin
		/// </summary>
		public	int			Margin				{ set { MarginTop = MarginBottom = MarginLeft = MarginRight = value; } }

		/// <summary>
		/// 
		/// </summary>
		public	int			MarginTop			{ get; set; } = 0;

		/// <summary>
		/// 
		/// </summary>
		public	int			MarginBottom		{ get; set; } = 0;

		/// <summary>
		/// 
		/// </summary>
		public	int			MarginLeft			{ get; set; } = 0;

		/// <summary>
		/// 
		/// </summary>
		public	int			MarginRight			{ get; set; } = 0;

		/// <summary>
		/// 
		/// </summary>
		public	int			Border				{ set { BorderTop = BorderBottom = BorderLeft = BorderRight = value; } }

		/// <summary>
		/// 
		/// </summary>
		public	virtual string		Text		{ get; set; }

		/// <summary>
		/// 
		/// </summary>
		public	Alignment	TextAlignment		{ get; set; }

		/// <summary>
		/// 
		/// </summary>
		public	int			TextOffsetX			{ get; set; }

		/// <summary>
		/// 
		/// </summary>
		public	int			TextOffsetY			{ get; set; }

		/// <summary>
		/// 
		/// </summary>
		public	TextEffect	TextEffect			{ get; set; }

		/// <summary>
		/// 
		/// </summary>
		public	float		TextTracking		{ get; set; }

		/// <summary>
		/// 
		/// </summary>
		public	FrameAnchor	Anchor			{ get; set; }


		public int				ImageOffsetX	{ get; set; }
		public int				ImageOffsetY	{ get; set; }
		public FrameImageMode	ImageMode		{ get; set; }
		public Color			ImageColor		{ get; set; }
		public Texture			Image			{ get; set; }

		/// <summary>
		/// 
		/// </summary>
		public LayoutEngine	Layout	{ 
			get { return layout; }
			set { layout = value; LayoutChanged?.Invoke( this, EventArgs.Empty ); }
		}

		LayoutEngine layout = null;


		#region	Events
		public class KeyEventArgs : EventArgs {
			public Keys	Key = Keys.None;
			public bool Shift = false;
			public bool Ctrl = false;
			public bool Alt = false;
			public char Symbol = '\0';
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
		public void Add ( Frame frame )
		{
			if ( !children.Contains(frame) ) {
				children.Add( frame );
				frame.parent	=	this;
				frame.OnStatusChanged( FrameStatus.None );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="frame"></param>
		public void Clear ()
		{
			foreach ( var child in children ) {
				child.parent = null;
			}
			children.Clear();
		}


		/// <summary>
		/// Inserts frame at specified index
		/// </summary>
		/// <param name="index"></param>
		/// <param name="frame"></param>
		public void Insert ( int index, Frame frame )
		{
			if ( !children.Contains(frame) ) {
				children.Insert( index, frame );
				frame.parent	=	this;
				frame.OnStatusChanged( FrameStatus.None );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="frame"></param>
		public void Remove ( Frame frame )
		{
			if ( this.children.Contains(frame) ) {
				this.children.Remove( frame );
				frame.parent	=	this;
			}
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
			KeyDown?.Invoke( this, new KeyEventArgs() { Key = key, Shift = shift, Alt = alt, Ctrl = ctrl } );
		}

		internal void OnKeyUp( Keys key, bool shift, bool alt, bool ctrl )
		{
			KeyUp?.Invoke( this, new KeyEventArgs() { Key = key, Shift = shift, Alt = alt, Ctrl = ctrl } );
		}

		internal void OnTypeWrite( Keys key, char symbol, bool shift, bool alt, bool ctrl )
		{
			TypeWrite?.Invoke( this, new KeyEventArgs() { Key = key, Symbol = symbol, Shift = shift, Alt = alt, Ctrl = ctrl } );
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
		internal void UpdateInternal ( GameTime gameTime )
		{
			var bfsList  = BFSList( this );
			var bfsListR = bfsList.ToList();
			bfsListR.Reverse();


			UpdateGlobalRect(0,0);

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

		int oldX = int.MinValue;
		int oldY = int.MinValue;
		int oldW = int.MinValue;
		int oldH = int.MinValue;
		bool firstResize = true;


		/// <summary>
		/// Checks move and resize and calls appropriate events
		/// </summary>
		protected void UpdateMove ()
		{
			if ( oldX != X || oldY != Y ) {	
				oldX = X;
				oldY = Y;
				if (Move!=null) {
					Move( this, new MoveEventArgs(){ X = X, Y = Y } );
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		protected void UpdateResize ()
		{
			if ( oldW != Width || oldH != Height ) {	

				if (Resize!=null) {
					Resize( this, new ResizeEventArgs(){ Width = Width, Height = Height } );
				}

				if (!firstResize) {
					ForEachChildren( f => f.UpdateAnchors( oldW, oldH, Width, Height ) );
				}

				firstResize = false;

				oldW = Width;
				oldH = Height;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="forceTransitions"></param>
		public virtual void RunLayout ()
		{
			layout?.RunLayout( this );

			foreach ( var child in Children ) {
				child.RunLayout();
			}

			layout?.RunLayout( this );
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


		}



		/// <summary>
		/// Gets global text rectangle without applied offsets
		/// </summary>
		/// <param name="start"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		protected Rectangle MeasureText ()
		{
			if (Text==null) {
				return new Rectangle(0,0,0,0);
			}

			float textWidth		=	8 * Text.Length;
			float textHeight	=	8;
			float capHeight		=	8;
			float lineHeight	=	8;
			float baseLine		=	8;

			if (Font!=null) {
				var r		=	Font.MeasureStringF( Text, TextTracking );
				textWidth	=	r.Width;
				textHeight	=	r.Height;
				baseLine	=	Font.BaseLine;
				capHeight	=	Font.CapHeight;
				lineHeight	=	Font.LineHeight;
			}

			int x	=	0;
			int y	=	0;
			var gp	=	GetPaddedRectangle();

			int hAlign	=	0;
			int vAlign	=	0;

			switch (TextAlignment) {
				case Alignment.TopLeft			: hAlign = -1; vAlign = -1; break;
				case Alignment.TopCenter		: hAlign =  0; vAlign = -1; break;
				case Alignment.TopRight			: hAlign =  1; vAlign = -1; break;
				case Alignment.MiddleLeft		: hAlign = -1; vAlign =  0; break;
				case Alignment.MiddleCenter		: hAlign =  0; vAlign =  0; break;
				case Alignment.MiddleRight		: hAlign =  1; vAlign =  0; break;
				case Alignment.BottomLeft		: hAlign = -1; vAlign =  1; break;
				case Alignment.BottomCenter		: hAlign =  0; vAlign =  1; break;
				case Alignment.BottomRight		: hAlign =  1; vAlign =  1; break;

				case Alignment.BaselineLeft		: hAlign = -1; vAlign =  2; break;
				case Alignment.BaselineCenter	: hAlign =  0; vAlign =  2; break;
				case Alignment.BaselineRight	: hAlign =  1; vAlign =  2; break;
			}

			if ( hAlign  < 0 )	x	=	gp.X + (int)( 0 );
			if ( hAlign == 0 )	x	=	gp.X + (int)( 0 + (int)( gp.Width/2 - textWidth/2 ) );
			if ( hAlign  > 0 )	x	=	gp.X + (int)( 0 + (int)( gp.Width - textWidth ) );

			if ( vAlign  < 0 )	y	=	gp.Y + (int)( 0 );
			if ( vAlign == 0 )	y	=	gp.Y + (int)( capHeight/2 - baseLine + gp.Height/2 );
			if ( vAlign  > 0 )	y	=	gp.Y + (int)( gp.Height - lineHeight );
			if ( vAlign == 2 )	y	=	gp.Y - (int)baseLine;

			return new Rectangle( x, y, (int)textWidth, (int)textHeight );
		}


		/// <summary>
		/// Draws string
		/// </summary>
		/// <param name="text"></param>
		protected virtual void DrawFrameText ( SpriteLayer spriteLayer, int clipRectIndex )
		{											
			if (string.IsNullOrEmpty(Text)) {
				return;
			}

			var rect = MeasureText();

			int x = rect.X;
			int y = rect.Y;

			if (Font!=null) {
				
				if (ShadowColor.A!=0) {
					Font.DrawString( spriteLayer, Text, x + TextOffsetX+ShadowOffset.X, y + TextOffsetY+ShadowOffset.Y, ShadowColor, clipRectIndex, TextTracking, false );
				}

				Font.DrawString( spriteLayer, Text, x + TextOffsetX, y + TextOffsetY, ForeColor, clipRectIndex, TextTracking, false );

			} else {

				if (ShadowColor.A!=0) {
					spriteLayer.DrawDebugString( x + TextOffsetX+ShadowOffset.X, y + TextOffsetY+ShadowOffset.Y, Text, ShadowColor, clipRectIndex );
				}

				spriteLayer.DrawDebugString( x + TextOffsetX, y + TextOffsetY, Text, ForeColor, clipRectIndex );

			}
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Animation stuff :
		 * 
		-----------------------------------------------------------------------------------------*/

		#if false
		List<ITransition>	transitions	=	new List<ITransition>();


		/// <summary>
		/// Pushes new transition to the chain of animation transitions.
		/// Origin value will be retrived when transition starts.
		/// When one of the newest transitions starts, previous transitions on same property will be terminated.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="I"></typeparam>
		/// <param name="property"></param>
		/// <param name="termValue"></param>
		/// <param name="delay"></param>
		/// <param name="period"></param>
		public void RunTransition<T,I> ( string property, T targetValue, int delay, int period ) where I: IInterpolator<T>, new()
		{
			var pi	=	GetType().GetProperty( property );
			
			if ( pi.PropertyType != typeof(T) ) {	
				throw new ArgumentException(string.Format("Bad property and types: {0} is {1}, but values are {2}", property, pi.PropertyType, typeof(T)) );
			}

			//	call ToList() to terminate LINQ evaluation :
			var toCancel = transitions.Where( t => t.TagName == property ).ToList();

			transitions.Add( new Transition<T,I>( this, pi, targetValue, delay, period, toCancel ){ TagName = property } );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="property"></param>
		/// <param name="targetValue"></param>
		/// <param name="delay"></param>
		/// <param name="period"></param>
		public void RunTransition ( string property, Color targetValue, int delay, int period )
		{
			RunTransition<Color, ColorInterpolator>( property, targetValue, delay, period );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="property"></param>
		/// <param name="targetValue"></param>
		/// <param name="delay"></param>
		/// <param name="period"></param>
		public void RunTransition ( string property, int targetValue, int delay, int period )
		{
			RunTransition<int, IntInterpolator>( property, targetValue, delay, period );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		void UpdateTransitions ( GameTime gameTime )
		{
			foreach ( var t in transitions ) {
				t.Update( gameTime );
			}

			transitions.RemoveAll( t => t.IsDone );
		}
		#endif


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Anchors :
		 * 
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// Incrementally preserving half offset
		/// </summary>
		/// <param name="oldV"></param>
		/// <param name="newV"></param>
		/// <param name="x"></param>
		/// <returns></returns>
		int SafeHalfOffset ( int oldV, int newV, int x )
		{
			int dw = newV - oldV;

			if ( (dw & 1)==1 ) {

				if ( dw > 0 ) {

					if ( (oldV&1)==1 ) {
						dw ++;
					}

				} else {

					if ( (oldV&1)==0 ) {
						dw --;
					}
				}

				return	x + dw/2;

			} else {
				return	x + dw/2;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="oldW"></param>
		/// <param name="oldH"></param>
		/// <param name="newW"></param>
		/// <param name="newH"></param>
		void UpdateAnchors ( int oldW, int oldH, int newW, int newH )
		{
			int dw	=	newW - oldW;
			int dh	=	newH - oldH;

			if ( !Anchor.HasFlag( FrameAnchor.Left ) && !Anchor.HasFlag( FrameAnchor.Right ) ) {
				X	=	SafeHalfOffset( oldW, newW, X );				
			}

			if ( !Anchor.HasFlag( FrameAnchor.Left ) && Anchor.HasFlag( FrameAnchor.Right ) ) {
				X	=	X + dw;
			}

			if ( Anchor.HasFlag( FrameAnchor.Left ) && !Anchor.HasFlag( FrameAnchor.Right ) ) {
			}

			if ( Anchor.HasFlag( FrameAnchor.Left ) && Anchor.HasFlag( FrameAnchor.Right ) ) {
				Width	=	Width + dw;
			}


		
			if ( !Anchor.HasFlag( FrameAnchor.Top ) && !Anchor.HasFlag( FrameAnchor.Bottom ) ) {
				Y	=	SafeHalfOffset( oldH, newH, Y );				
			}

			if ( !Anchor.HasFlag( FrameAnchor.Top ) && Anchor.HasFlag( FrameAnchor.Bottom ) ) {
				Y	=	Y + dh;
			}

			if ( Anchor.HasFlag( FrameAnchor.Top ) && !Anchor.HasFlag( FrameAnchor.Bottom ) ) {
			}

			if ( Anchor.HasFlag( FrameAnchor.Top ) && Anchor.HasFlag( FrameAnchor.Bottom ) ) {
				Height	=	Height + dh;
			}
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Layouting :
		 * 
		-----------------------------------------------------------------------------------------*/

	}
}

