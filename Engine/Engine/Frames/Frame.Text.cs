using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Input;
using Fusion.Engine.Graphics;
using Fusion.Engine.Common;
using Forms = System.Windows.Forms;


namespace Fusion.Engine.Frames {

	public partial class Frame {

		string		textString = "";
		SpriteFont	textFont;
		Alignment	textAlignment;
		bool		textDirty = true;
		string[]	textLines;
		Size2[]		textSizes;
		int			textBaseline;
		int			textCapHeight;
		int			textLineHeight;
		Size2		textBlockSize;



		/// <summary>
		/// Sets and gets text font. Could be null.
		/// If null standart 8x8 font will be used.
		/// </summary>
		public	SpriteFont	Font { 
			get {
				return textFont;
			} 
			set {
				if (textFont!=value) {
					textFont  = value;
					textDirty = true;
				}
			}
		}


		/// <summary>
		/// Gets text block size
		/// </summary>
		public Size2 TextBlockSize {
			get {
				RefreshTextMetrics();
				return textBlockSize;
			}
		}


		/// <summary>
		/// Sets and gets frame's text
		/// </summary>
		public virtual string Text { 
			get {
				return textString;
			}
			set {
				//	dirty hack
				var newVal = value ?? "";
				if (textString!= newVal) {
					textString = newVal;
					textDirty  = true;
				}
			}
		}


		/// <summary>
		/// Gets and sets text alignment
		/// </summary>
		public	Alignment TextAlignment { 
			get {
				return textAlignment;
			}
			set {
				if (textAlignment!=value) {
					textAlignment = value;
					textDirty = true;
				}
			}
		}

		/// <summary>
		/// Gets and sets text offset along X-axis
		/// </summary>
		public	int TextOffsetX { get; set; }

		/// <summary>
		/// Gets and sets text offset along Y-axis
		/// </summary>
		public	int TextOffsetY { get; set; }



		/// <summary>
		/// Updates text metrics and cached data
		/// </summary>
		void RefreshTextMetrics ()
		{
			if (!textDirty) {
				return;
			}

			textLines		=	textString.SplitLines();

			textBaseline	=	(Font==null) ? 8 : Font.BaseLine;
			textCapHeight	=	(Font==null) ? 8 : Font.CapHeight;
			textLineHeight	=	(Font==null) ? 8 : Font.LineHeight;

			textSizes		=	textLines
								.Select( line => MeasureSingleLineString( textFont, line ) )
								.ToArray();

			textBlockSize	=	new Size2( textSizes.Max( sz1 => sz1.Width ), textSizes.Sum( sz2 => sz2.Height ) );

			textDirty = false;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="lineIndex"></param>
		/// <returns></returns>
		Size2 MeasureSingleLineString ( SpriteFont font, string lineText )
		{
			if (lineText==null) {
				throw new ArgumentNullException("lineText");
			}

			float textWidth		=	8 * lineText.Length;
			float textHeight	=	8 * 1;

			if (Font!=null) {
				var r		=	Font.MeasureStringF( lineText );
				textWidth	=	r.Width;
				textHeight	=	r.Height;
			} 

			return new Size2( (int)textWidth, (int)textHeight );
		}



		void DecodeAlignment ( Alignment alignment, out int hAlign, out int vAlign )
		{
			hAlign = vAlign = 0;
			switch (alignment) {
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
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Rectangle ComputeGlobalAlignedTextRectangle ()
		{
			RefreshTextMetrics();

			int x			=	0;
			int y			=	0;
			int textWidth	=	textBlockSize.Width;
			int textHeight	=	textBlockSize.Height;
			int baseLine	=	textBaseline;
			int lineHeight	=	textLineHeight;
			int capHeight	=	textCapHeight;
			var gp			=	GetPaddedRectangle();

			int hAlign	=	0;
			int vAlign	=	0;

			DecodeAlignment( TextAlignment, out hAlign, out vAlign );

			if ( hAlign  < 0 )	x	=	gp.X + ( 0 );
			if ( hAlign == 0 )	x	=	gp.X + ( 0 + ( gp.Width/2 - textWidth/2 ) );
			if ( hAlign  > 0 )	x	=	gp.X + ( 0 + ( gp.Width - textWidth ) );

			#warning Middle for raster fonts?
			if ( vAlign  < 0 )	y	=	gp.Y + ( 0 );
			if ( vAlign == 0 )	y	=	gp.Y + ( gp.Height - textHeight ) / 2;  
			if ( vAlign  > 0 )	y	=	gp.Y + ( gp.Height - textHeight );
			if ( vAlign == 2 )	y	=	gp.Y - baseLine;

			return new Rectangle( x, y, textBlockSize.Width, textBlockSize.Height );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="spriteLayer"></param>
		/// <param name="text"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="color"></param>
		/// <param name="clipRectIndex"></param>
		void DrawTextLine ( SpriteLayer spriteLayer, string text, float x, float y, Color color, int clipRectIndex )
		{
			if (textFont!=null) {
				textFont.DrawString( spriteLayer, text, x, y, color, clipRectIndex, 0, false, false );
			} else {
				spriteLayer.DrawDebugString( x,y, text, color, clipRectIndex );
			}
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

			int hAlign;
			int vAlign;

			DecodeAlignment( TextAlignment, out hAlign, out vAlign );

			var rect = ComputeGlobalAlignedTextRectangle();

			var x  = rect.X;
			var y  = rect.Y;
			var dx = TextOffsetX;
			var dy = TextOffsetY;
			var sx = TextOffsetX + ShadowOffset.X;
			var sy = TextOffsetX + ShadowOffset.Y;


			for ( int index = 0; index < textLines.Length; index ++ ) {

				var text	=	textLines[ index ];
				var size	=	textSizes[ index ];
				int indent	=	0;

				if (hAlign==-1) indent = 0;
				if (hAlign== 0) indent = (textBlockSize.Width - size.Width) / 2;
				if (hAlign== 1) indent = (textBlockSize.Width - size.Width) / 1;

				int xpos	=	x + indent;
				int ypos	=	y + index * textLineHeight;

				if (ShadowColor.A!=0) {
					DrawTextLine( spriteLayer, text, xpos + sx, ypos + sy, ShadowColor, clipRectIndex );
				}

				DrawTextLine( spriteLayer, text, xpos + dx, ypos + dy, ForeColor, clipRectIndex );

			}
		}
	}
}

