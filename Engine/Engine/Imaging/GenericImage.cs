using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core.Mathematics;


namespace Fusion.Engine.Imaging {
	public partial class GenericImage<TColor> {

		public int	Width	{ get; protected set; }
		public int	Height	{ get; protected set; }

		public TColor[]	RawImageData { get; protected set; }

		public object Tag { get; set; }
		

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="width">Image width</param>
		/// <param name="height">Image height</param>
		public GenericImage ( int width, int height )
		{
			RawImageData	=	new TColor[width*height];

			Width	=	width;
			Height	=	height;

			if (Width<=0) {
				throw new ArgumentOutOfRangeException("Image width must be > 0");
			}

			if (Height<=0) {
				throw new ArgumentOutOfRangeException("Image height must be > 0");
			}
		}



		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="width">Image width</param>
		/// <param name="height">Image height</param>
		/// <param name="fillColor">Color to fill image</param>
		public GenericImage ( int width, int height, TColor fillColor )
		{
			RawImageData	=	new TColor[width*height];

			Width	=	width;
			Height	=	height;

			if (Width<=0) {
				throw new ArgumentOutOfRangeException("Image width must be > 0");
			}

			if (Height<=0) {
				throw new ArgumentOutOfRangeException("Image height must be > 0");
			}

			for (int i=0; i<RawImageData.Length; i++) {
				RawImageData[i]	=	fillColor;
			}
		}




		/// <summary>
		/// Returns address of pixel with given coordinates and adressing mode
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <param name="wrap"></param>
		/// <returns></returns>
		public int Address ( int x, int y, bool wrap = true )
		{
			if (wrap) {
				x =	Wrap( x, Width );
				y =	Wrap( y, Height );
			} else {
				x	=	Clamp( x, 0, Width - 1 );
				y	=	Clamp( y, 0, Height - 1 );
			}
			return x + y * Width;
		}



		/// <summary>
		/// Sets and gets pixel color at given coordinates.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public TColor this[int x, int y] {
			get {
				return Sample(x, y, false);
			}
			
			set {
				SetPixel(x,y, value, false);
			}	

		}



		/// <summary>
		/// Sets and gets pixel color at given coordinates.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public TColor this[Int2 xy] {
			get {
				return Sample(xy.X, xy.Y, false);
			}
			
			set {
				SetPixel(xy.X, xy.Y, value, false);
			}	

		}



		/// <summary>
		/// Fills image with 
		/// </summary>
		/// <param name="seed"></param>
		/// <param name="monochrome"></param>
		public void Fill ( TColor color )
		{
			PerpixelProcessing( p => color );
		}


		/// <summary>
		/// Samples image at given coordinates with wraping addressing mode
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public TColor SampleWrap ( int x, int y )
		{
			x = Wrap(x, Width);
			y = Wrap(y, Height);
			var a = x + y * Width;
			return RawImageData[ a ];
		}


		/// <summary>
		/// Samples image at given coordinates with clamping addressing mode
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public TColor SampleClamp ( int x, int y )
		{
			x	=	Clamp( x, 0, Width - 1 );
			y	=	Clamp( y, 0, Height - 1 );
			var a = x + y * Width;
			return RawImageData[ a ];
		}



		/// <summary>
		/// Samples image at given coordinates with wraping addressing mode
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public TColor Sample ( int x, int y, bool wrap = true)
		{
			return RawImageData[ Address( x, y, wrap ) ];
		}


		/// <summary>
		/// Samples image at given coordinates with wraping addressing mode
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public TColor SampleMip ( int x, int y, Func<TColor,TColor,float,TColor> lerpFunc )
		{
			var c00 = RawImageData[ Address( x*2+0, y*2+0, true ) ];
			var c01 = RawImageData[ Address( x*2+0, y*2+1, true ) ];
			var c10 = RawImageData[ Address( x*2+1, y*2+0, true ) ];
			var c11 = RawImageData[ Address( x*2+1, y*2+1, true ) ];

			var c0x	= lerpFunc( c00, c01, 0.5f );
			var c1x	= lerpFunc( c10, c11, 0.5f );

			return lerpFunc( c0x, c1x, 0.5f );
		}



		/// <summary>
		/// Samples average of four neighbouring texels with given top-left corener with clamp addressing mode
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public TColor SampleQ4Clamp ( int x, int y, Func<TColor,TColor,float,TColor> lerpFunc )
		{
			var c00 = RawImageData[ Address( x+0, y+0, false ) ];
			var c01 = RawImageData[ Address( x+0, y+1, false ) ];
			var c10 = RawImageData[ Address( x+1, y+0, false ) ];
			var c11 = RawImageData[ Address( x+1, y+1, false ) ];

			var c0x	= lerpFunc( c00, c01, 0.5f );
			var c1x	= lerpFunc( c10, c11, 0.5f );

			return lerpFunc( c0x, c1x, 0.5f );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="img"></param>
		public void CopyTo ( GenericImage<TColor> destination )
		{
			var w = Math.Min( Width, destination.Width );
			var h = Math.Min( Height, destination.Height );

			for (int x=0; x<w; x++) {
				for (int y=0; y<h; y++) {
					destination[x,y] = this[x,y];
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="img"></param>
		public void CopySubImageTo ( int srcX, int srcY, int srcWidth, int srcHeight, int dstX, int dstY, GenericImage<TColor> destination )
		{
			for (int x=0; x<srcWidth; x++) {
				for (int y=0; y<srcHeight; y++) {
					destination.SetPixel( dstX + x, dstY + y, Sample( srcX+x, srcY+y ) );
				}
			}
		}



		/// <summary>
		/// Sample with filtering
		/// </summary>
		/// <param name="x">value within range 0..1</param>
		/// <param name="y">value within range 0..1</param>
		/// <param name="wrap"></param>
		/// <returns></returns>
		public TColor Sample ( float x, float y, Func<TColor,TColor,float,TColor> lerpFunc, bool wrap = true )
		{
			var	tx	=	Frac( x * Width );
			var	ty	=	Frac( y * Height );
			int	x0	=	Wrap( (int)(x * Width)		, Width );
			int	x1	=	Wrap( (int)(x * Width + 1)	, Width );
			int	y0	=	Wrap( (int)(y * Height)		, Height );
			int	y1	=	Wrap( (int)(y * Height + 1) , Height );
			
			//   xy
			var v00	=	Sample( x0, y0, wrap );
			var v01	=	Sample( x0, y1, wrap );
			var v10	=	Sample( x1, y0, wrap );
			var v11	=	Sample( x1, y1, wrap );

			var v0x	=	lerpFunc( v00, v01, ty );
			var v1x	=	lerpFunc( v10, v11, ty );
			return		lerpFunc( v0x, v1x, tx );
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="w"></param>
		/// <param name="h"></param>
		/// <param name="color"></param>
		public void DrawRectangle ( int x, int y, int w, int h, TColor color )
		{
			for (var i=x; i<x+w; i++) {
				for (var j=y; j<y+h; j++) {
					SetPixel( i,j, color );	
				}
			}
		}


		/// <summary>
		/// Writes pixel to image
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <param name="color"></param>
		/// <param name="wrap"></param>
		public void SetPixel ( int x, int y, TColor value, bool wrap = true )
		{
			RawImageData[ Address( x, y, wrap ) ] = value;
		}


		
		/// <summary>
		/// Does perpixel processing with given function
		/// </summary>
		/// <param name="procFunc"></param>
		public void PerpixelProcessing ( Func<int, int, TColor, TColor> procFunc )
		{
			for (int x=0; x<Width; x++) 
			for (int y=0; y<Height; y++)  
				SetPixel(x,y, procFunc( x, y, Sample( x,y ) ) );
		}


		/// <summary>
		/// Does perpixel processing with given function
		/// </summary>
		/// <param name="procFunc"></param>
		public void PerpixelProcessing ( Func<TColor, TColor> procFunc )
		{
			for (int i=0; i<RawImageData.Length; i++) {
				RawImageData[i] = procFunc( RawImageData[i] );
			}
		}



		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Simple Math :
		 * 
		-----------------------------------------------------------------------------------------*/

		public static int Clamp ( int x, int min, int max ) 
		{
			if (x < min) return min;
			if (x > max) return max;
			return x;
		}



		public static int Wrap ( int x, int wrapSize ) 
		{
			if ( x<0 ) {
				x = x % wrapSize + wrapSize;
			}
			return	x % wrapSize;
		}



		public static float Frac ( float x )
		{
			return x < 0 ? x%1+1 : x%1;
		}



		public static float Lerp ( float a, float b, float x ) 
		{
			return a*(1-x) + b*x;
		}

		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Filters
		 * 
		-----------------------------------------------------------------------------------------*/

		public void Dilate ( GenericImage<TColor> temp, Func<Int2,bool> predicate )
		{
			if (temp.Width!=Width) {
				throw new ArgumentException("temp.Width!=Width");
			}
			if (temp.Height!=Height) {
				throw new ArgumentException("temp.Height!=Height");
			}

			Int2[] offsets = new[] { 
				new Int2(+1,+0),
				new Int2(-1,+0),
				new Int2(+0,+1),
				new Int2(+0,-1),
				new Int2(+1,+1),
				new Int2(-1,-1),
				new Int2(+1,-1),
				new Int2(-1,+1),
			};


			for ( int i=0; i<Width; i++ ) {

				for ( int j=0; j<Height; j++ ) {

					if (!predicate(new Int2(i,j))) {

						for (int k=0; k<8; k++) {

							Int2 xy = new Int2( i + offsets[k].X, j + offsets[k].Y );

							if (predicate(xy)) {
								temp[i,j] = this[xy];
							}

						}

					} else {

						temp[i,j] = this[i,j];

					}
				}
			}

			temp.CopyTo( this );
		}
	}

}
