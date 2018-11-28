using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using SharpDX;
using Fusion.Drivers.Graphics;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;


namespace Fusion.Engine.Graphics {
	public partial class SpriteFont : DisposableBase {


		public class Glyph {
			public Rectangle	DstRect;
			public Rectangle	SrcRect;
			public DiscTexture	Texture;
			public Color		Color;
		}


		struct SpriteFontInfo {
			public struct CharInfo {
				public bool			validChar;
				public int			xAdvance;
				public RectangleF	srcRect;
				public RectangleF	dstRect;
			}

		
			//public string		fontImage;
			public string		fontFace;
			public int			lineHeight;
			public int			baseLine;
			public int			scaleWidth, scaleHeight;
			public CharInfo[]	charInfo;
			public Dictionary<Tuple<char,char>, int>	kernings; 
		}

		SpriteFontInfo	fontInfo;
		UserTexture		fontTexture;
		GraphicsDevice	rs;

		/// <summary>
		/// Font's baseline.
		/// </summary>
		public int BaseLine		{ get { return fontInfo.baseLine; } }

		/// <summary>
		/// Space with.
		/// </summary>
		public int SpaceWidth	{ get; private set; }

		/// <summary>
		/// Font's line height.
		/// </summary>
		public int LineHeight	{ get; private set; }

		/// <summary>
		/// Approximate capital letter heights. Measured using median.
		/// </summary>
		public int CapHeight { get; private set; }

	
		/// <summary>
		/// Constrcutor
		/// </summary>
		/// <param name="device"></param>
		/// <param name="fileName"></param>
		public SpriteFont ( RenderSystem rs, Stream stream )
		{
			this.rs	=	rs.Device;

			using (var br = new BinaryReader(stream)) {

				var xml = br.ReadString();
				FontFile input = FontLoader.LoadFromString( xml );

				int numGlyphs	=	input.Chars.Max( ch => ch.ID );

				//	create charInfo and kernings :
				fontInfo.kernings = new Dictionary<Tuple<char,char>, int>();
				fontInfo.charInfo = new SpriteFontInfo.CharInfo[numGlyphs+1];

				//	check one-page bitmap fonts :
				if (input.Pages.Count!=1) {
					throw new GraphicsException("Only one page of font image is supported");
				}

				//	create path for font-image :
				string fontImagePath	=	input.Pages[0].File;

				//	skip two bytes :
				var texData				=	stream.ReadAllBytes();
				fontTexture				=	new UserTexture( rs.Game.RenderSystem, texData, false );
			
				//	Fill structure :
				fontInfo.fontFace		=	input.Info.Face;
				fontInfo.baseLine		=	input.Common.Base;
				fontInfo.lineHeight		=	input.Common.LineHeight;
				fontInfo.scaleWidth		=	input.Common.ScaleW;
				fontInfo.scaleHeight	=	input.Common.ScaleH;

				float scaleWidth = fontInfo.scaleWidth;
				float scaleHeight = fontInfo.scaleHeight;

				//	process character info :
				for ( int i=0; i<input.Chars.Count; i++) {
					FontChar ch = input.Chars[i];

					int id = ch.ID;

					if (id<0) continue;

					int x = ch.X;
					int y = ch.Y;
					int xoffs = ch.XOffset;
					int yoffs = ch.YOffset;
					int w = ch.Width;
					int h = ch.Height;

					fontInfo.charInfo[ ch.ID ].validChar	=	true;
					fontInfo.charInfo[ ch.ID ].xAdvance		=	ch.XAdvance;
					fontInfo.charInfo[ ch.ID ].srcRect		=	new RectangleF(x, y, w, h);
					fontInfo.charInfo[ ch.ID ].dstRect		=	new RectangleF(xoffs, yoffs, w, h);
				}


				var letterHeights = input.Chars
						.Where( ch1 => char.IsUpper( (char)(ch1.ID) ) )
						.Select( ch2 => ch2.Height )
						.OrderBy( h => h )
						.ToList();
				CapHeight	=	letterHeights[ letterHeights.Count/2 ];



				//	process kerning info :
				for ( int i=0; i<input.Kernings.Count; i++) {
					var pair	=	new Tuple<char,char>( (char)input.Kernings[i].First, (char)input.Kernings[i].Second);
					int kerning =	input.Kernings[i].Amount;
					fontInfo.kernings.Add( pair, kerning );
				}

				SpaceWidth	=	MeasureString(" ").Width;
				LineHeight	=	MeasureString(" ").Height;
			}
		}


		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref fontTexture );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="ch"></param>
		/// <returns></returns>
		SpriteFontInfo.CharInfo GetInfo ( char ch )
		{
			if (ch>=fontInfo.charInfo.Length) {
				return new SpriteFontInfo.CharInfo();
			} else {
				return fontInfo.charInfo[ch];
			}
		}



		/// <summary>
		/// Gets character index under cursor in single line
		/// </summary>
		/// <param name="text"></param>
		/// <param name="cursorPixelPos"></param>
		/// <returns></returns>
		public int FindIndexUnderCursor ( string text, int cursorPixelPos )
		{
			int	x = 0;
			int	length = text.Length;
			
			for (int i=0; i<length; i++) {

				char ch0	= text[i];
				char ch1	= (i+1)<length ? text[i+1] : '\0';

				var chi		= GetInfo(ch0);
				var chPair	= new Tuple<char,char>(ch0,ch1);
				var kerning = GetKerning( ch0, ch1 );

				if (cursorPixelPos < x + chi.xAdvance/2) {
					return i;
				}

				x += chi.xAdvance;
				x += kerning;
			}

			return length;
		}

		
		/// <summary>
		/// Measures multiline string 
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public RectangleF MeasureStringMultiline ( string text ) 
		{
			int		width = 0;
			int		line = 1;
			float	maxWidth = 0;
			int		length = text.Length;
			
			for (int i=0; i<length; i++) {

				char ch0	= text[i];
				char ch1	= (i+1)<length ? text[i+1] : '\0';
				
				if (ch0 == '\n') {
					line++;
					maxWidth = Math.Max( maxWidth, width );
				}

				var chi		= GetInfo(ch0);
				var chPair	= new Tuple<char,char>(ch0,ch1);
				var kerning = GetKerning( ch0, ch1 );

				width += chi.xAdvance;
				width += kerning;

				maxWidth = Math.Max( maxWidth, width );
			}
			return new RectangleF( 0, 0, maxWidth, line * fontInfo.lineHeight );
		}

		
		/// <summary>
		/// Measures string single line substring
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public RectangleF MeasureSubstring ( string text, int start=0, int end = int.MaxValue ) 
		{
			int		left = 0;
			int		right = 0;
			int		length = Math.Min( text.Length, end );
			
			for (int i=0; i<length; i++) {

				if (i<start) {
					left = right;
				}

				char ch0	= text[i];
				char ch1	= (i+1)<length ? text[i+1] : '\0';

				var chi		= GetInfo(ch0);
				var chPair	= new Tuple<char,char>(ch0,ch1);
				var kerning = GetKerning( ch0, ch1 );

				right += chi.xAdvance;
				right += kerning;
			}

			var rect = new Rectangle();

			rect.Left	=	left;
			rect.Right	=	right;
			rect.Bottom	=	fontInfo.lineHeight;
			rect.Top	=	0;

			return rect;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="ch"></param>
		/// <returns></returns>
		public Rectangle MeasureGlyph ( char ch )
		{
			var r = GetInfo(ch).dstRect;
			return new Rectangle( (int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public Rectangle MeasureString( string text )
		{
			var rectF = MeasureSubstring( text );
			return new Rectangle( (int)rectF.X, (int)rectF.Y, (int)rectF.Width, (int)rectF.Height );
		}


		/*Color EscColor ( char ch ) {
			if (ch=='0') return Color.White;
			if (ch=='1') return Color.Red;
			if (ch=='2') return Color.Orange;
			if (ch=='3') return Color.Yellow;
			if (ch=='4') return Color.Green;
			if (ch=='5') return Color.Cyan;
			if (ch=='6') return Color.Blue;
			if (ch=='7') return Color.Magenta;
			if (ch=='8') return Color.Black;
			if (ch=='9') return Color.Gray;
			return Color.White;
		} */


		/// <summary>
		/// Draws the string
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="text"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="color"></param>
		public void DrawString( SpriteLayer spriteBatch, string text, float xPos, float yPos, Color color, int frameIndex=0, float tracking = 0, bool useBaseLine = true, bool flip = false ) 
		{
			if (text==null) {
				return;
			}

			float x, y;

			if (!flip) {
				x = xPos;
				y = yPos;// - fontInfo.baseLine;
				if (useBaseLine) {
					y -= fontInfo.baseLine;
				}
			} else {
				x = yPos;
				y = xPos;// - fontInfo.baseLine;
				if (useBaseLine) {
					x -= fontInfo.baseLine;
				}
			}


			int length = text.Length;

			float w	=	fontTexture.Width;
			float h	=	fontTexture.Height;

			for (int i=0; i<length; i++) {

				char ch0	= text[i];

				if (ch0 == '\n') {
					y += fontInfo.lineHeight;
					x =  xPos;
				}

				char ch1	= (i+1)<length ? text[i+1] : '\0';
				var chi		= GetInfo(ch0);
				var kerning = GetKerning( ch0, ch1 );

				/*if (ch0=='^' && char.IsDigit(ch1)) {
					i++;
					color = EscColor(ch1);
					continue;
				} */

				RectangleF	dstRect = chi.dstRect;
				RectangleF	srcRect = chi.srcRect;

				dstRect.X	+= x;
				dstRect.Y	+= y;
				/*dstRect.Right	+= x;
				dstRect.Bottom  += y;*/
				if (!flip) {
					x += chi.xAdvance;
					x += kerning;
					x += tracking;
				} else {
					x -= chi.xAdvance;
					x -= kerning;
					x -= tracking;
				}

				//spriteBatch.Draw( fontTexture, dstRect, srcRect, color );
				var c = color;// * spriteBatch.ColorMultiplier;

				if (!flip) {
					var v0	=	new SpriteVertex( new Vector3(dstRect.X,                 dstRect.Y                 , 0), c, new Vector2((srcRect.X                ) / w, (srcRect.Y                 ) / h), frameIndex );
					var v1	=	new SpriteVertex( new Vector3(dstRect.X + dstRect.Width, dstRect.Y                 , 0), c, new Vector2((srcRect.X + srcRect.Width) / w, (srcRect.Y                 ) / h), frameIndex );
					var v2	=	new SpriteVertex( new Vector3(dstRect.X + dstRect.Width, dstRect.Y + dstRect.Height, 0), c, new Vector2((srcRect.X + srcRect.Width) / w, (srcRect.Y + srcRect.Height) / h), frameIndex );
					var v3	=	new SpriteVertex( new Vector3(dstRect.X,                 dstRect.Y + dstRect.Height, 0), c, new Vector2((srcRect.X                ) / w, (srcRect.Y + srcRect.Height) / h), frameIndex );
					spriteBatch.DrawQuad( fontTexture, v0, v1, v2, v3 );
				} else {																		                   
					var v0	=	new SpriteVertex( new Vector3(dstRect.Y                 , dstRect.X                , 0), c, new Vector2((srcRect.X                ) / w, (srcRect.Y                 ) / h), frameIndex );
					var v1	=	new SpriteVertex( new Vector3(dstRect.Y                 , dstRect.X - dstRect.Width, 0), c, new Vector2((srcRect.X + srcRect.Width) / w, (srcRect.Y                 ) / h), frameIndex );
					var v2	=	new SpriteVertex( new Vector3(dstRect.Y + dstRect.Height, dstRect.X - dstRect.Width, 0), c, new Vector2((srcRect.X + srcRect.Width) / w, (srcRect.Y + srcRect.Height) / h), frameIndex );
					var v3	=	new SpriteVertex( new Vector3(dstRect.Y + dstRect.Height, dstRect.X                , 0), c, new Vector2((srcRect.X                ) / w, (srcRect.Y + srcRect.Height) / h), frameIndex );
					spriteBatch.DrawQuad( fontTexture, v0, v1, v2, v3 );
				}

			}
		}
		


		/// <summary>
		/// 
		/// </summary>
		/// <param name="spriteBatch"></param>
		/// <param name="glyphs"></param>
		/// <param name="offsetX"></param>
		/// <param name="offsetY"></param>
		/*static public void DrawGlyphs ( SpriteBatch spriteBatch, IEnumerable<Glyph> glyphs, int offsetX, int offsetY )
		{
			foreach ( var glyph in glyphs ) {
				spriteBatch.Draw( glyph.Texture, glyph.DstRect, glyph.SrcRect, offsetX, offsetY, glyph.Color );
			}
		} */



		/// <summary>
		/// 
		/// </summary>
		/// <param name="ch0"></param>
		/// <param name="ch1"></param>
		/// <returns></returns>
		int GetKerning ( char ch0, char ch1 )
		{
			var chPair	= new Tuple<char,char>(ch0,ch1);

			if ( fontInfo.kernings.ContainsKey( chPair ) ) {
				return fontInfo.kernings[ chPair ];
			}
			return 0;
		}
		
	}
}
