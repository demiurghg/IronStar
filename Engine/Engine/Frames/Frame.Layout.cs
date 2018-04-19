using System;
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

		int x;
		int y;
		int width;
		int height;
		int paddingLeft;
		int paddingRight;
		int paddingTop;
		int paddingBottom;
		int borderLeft;
		int borderRight;
		int borderTop;
		int borderBottom;
		int marginLeft;
		int marginRight;
		int marginTop;
		int marginBottom;

		bool layoutDirty = true;
		bool sizeDirty = true;
		bool moveDirty = true;

		public void MakeLayoutDirty()
		{
			layoutDirty = true;
		}


		#region SIZE & PLACEMENT

		/// <summary>
		/// Local X position of the frame
		/// </summary>
		public int X { 
			get { return x; }
			set { 
				if (x!=value) {
					x = value; 
					parent?.MakeLayoutDirty();
					moveDirty = true;
				}
			}
		}

		/// <summary>
		/// Local Y position of the frame
		/// </summary>
		public int Y {
			get { return y; }
			set { 
				if (y!=value) {
					y = value; 
					parent?.MakeLayoutDirty();
					moveDirty = true;
				}
			}
		}

		/// <summary>
		///	Width of the frame
		/// </summary>
		public virtual int Width {
			get { return width; }
			set {
				if (width!=value) { 
					width = value; 
					MakeLayoutDirty();
					parent?.MakeLayoutDirty();
					sizeDirty = true;
				}
			}
		}

		/// <summary>
		///	Height of the frame
		/// </summary>
		public virtual int Height {
			get { return height; }
			set { 
				if (height!=value) {
					height = value; 
					MakeLayoutDirty();
					parent?.MakeLayoutDirty();
					sizeDirty = true;
				}
			}
		}

		#endregion

		#region PADDING
		/// <summary>
		/// Left gap between frame and its content
		/// </summary>
		public int PaddingLeft { 
			get { return paddingLeft; }
			set { 
				if (paddingLeft!=value) {
					paddingLeft = value; 
					MakeLayoutDirty();
				}
			}
		}

		/// <summary>
		/// Right gap between frame and its content
		/// </summary>
		public int PaddingRight {
			get { return paddingRight; }
			set { 
				if (paddingRight!=value) {
					paddingRight = value; 
					MakeLayoutDirty();
				}
			}
		}

		/// <summary>
		/// Top gap  between frame and its content
		/// </summary>
		public int PaddingTop {
			get { return paddingTop; }
			set { 
				if (paddingTop!=value) {
					paddingTop = value; 
					MakeLayoutDirty();
				}
			}
		}

		/// <summary>
		/// Bottom gap  between frame and its content
		/// </summary>
		public int PaddingBottom {
			get { return paddingBottom; }
			set { 
				if (paddingBottom!=value) {
					paddingBottom = value; 
					MakeLayoutDirty();
				}
			}
		}

		/// <summary>
		/// Top, bottom, left and right padding
		/// </summary>
		public int Padding { 
			set { PaddingBottom = PaddingTop = PaddingLeft = paddingRight = value; } 
			get {
				if ( PaddingLeft==PaddingRight && PaddingTop==PaddingBottom && PaddingBottom==PaddingRight ) {
					return PaddingLeft;
				} else {
					return -1;
				}
			}
		}
		#endregion

		#region BORDER
		/// <summary>
		/// Left gap between frame and its content
		/// </summary>
		public int BorderLeft { 
			get { return borderLeft; }
			set { 
				if (borderLeft!=value) {
					borderLeft = value; 
					MakeLayoutDirty();
				}
			}
		}

		/// <summary>
		/// Right gap between frame and its content
		/// </summary>
		public int BorderRight {
			get { return borderRight; }
			set { 
				if (borderRight!=value) {
					borderRight = value; 
					MakeLayoutDirty();
				}
			}
		}

		/// <summary>
		/// Top gap  between frame and its content
		/// </summary>
		public int BorderTop {
			get { return borderTop; }
			set {
				if (borderTop!=value) { 
					borderTop = value; 
					MakeLayoutDirty();
				}
			}
		}

		/// <summary>
		/// Bottom gap  between frame and its content
		/// </summary>
		public int BorderBottom {
			get { return borderBottom; }
			set { 
				if (borderBottom!=value) {
					borderBottom = value; 
					MakeLayoutDirty();
				}
			}
		}

		/// <summary>
		/// Top, bottom, left and right border
		/// </summary>
		public int Border { 
			set { BorderBottom = BorderTop = BorderLeft = borderRight = value; } 
			get {
				if ( BorderLeft==BorderRight && BorderTop==BorderBottom && BorderBottom==BorderRight ) {
					return BorderLeft;
				} else {
					return -1;
				}
			}
		}
		#endregion

		#region MARGIN
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
		#endregion



		/// <summary>
		/// Gets and sets layout engine
		/// </summary>
		public LayoutEngine	Layout	{ 
			get { return layout; }
			set { 
				if (layout!=value) {
					layout = value;
					MakeLayoutDirty();
				}
			}
		}

		LayoutEngine layout = null;


		/// <summary>
		/// 
		/// </summary>
		public	FrameAnchor	Anchor			{ get; set; }

		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Layout Engine :
		 * 
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// Force layout engine to arrange child frames.
		/// </summary>
		/// <param name="forceTransitions"></param>
		public virtual void RunLayout ()
		{
			layout?.RunLayout( this );
		}



		void RunLayoutInternal ()
		{
			if (layoutDirty) {
				RunLayout();
				layoutDirty = false;
			}
		}


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
	}
}

