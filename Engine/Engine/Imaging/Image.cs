using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core.Mathematics;


namespace Fusion.Engine.Imaging {
	public partial class Image {

		public int	Width	{ get; protected set; }
		public int	Height	{ get; protected set; }

		public byte[]	RawImageData { get; protected set; }

		public object Tag { get; set; }

		public int PixelCount {
			get {
				return Width * Height;
			}
		}
		

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="width">Image width</param>
		/// <param name="height">Image height</param>
		public Image ( int width, int height )
		{
			RawImageData	=	new byte[width*height*4];

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
		/// 
		/// </summary>
		/// <param name="image"></param>
		public Image ( GenericImage<Color> image )
		{
			Width			=	image.Width;
			Height			=	image.Height;

			RawImageData	=	new byte[Width*Height*4];

			for (int i=0; i<PixelCount; i++) {
				SetPixelLinear( i, image.RawImageData[i] );
			}
		}


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="width">Image width</param>
		/// <param name="height">Image height</param>
		/// <param name="fillColor">Color to fill image</param>
		public Image ( int width, int height, Color fillColor )
		{
			RawImageData	=	new byte[width*height*4];

			Width	=	width;
			Height	=	height;

			if (Width<=0) {
				throw new ArgumentOutOfRangeException("Image width must be > 0");
			}

			if (Height<=0) {
				throw new ArgumentOutOfRangeException("Image height must be > 0");
			}

			for (int i=0; i<PixelCount; i++) {
				SetPixelLinear( i, fillColor );
			}
		}




		/// <summary>
		/// Returns address of pixel with given coordinates and adressing mode
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <param name="wrap"></param>
		/// <returns></returns>
		public int ComputeByteAddress ( int x, int y )
		{
			x	=	Clamp( x, 0, Width - 1 );
			y	=	Clamp( y, 0, Height - 1 );

			return (x + y * Width) * 4;
		}



		/// <summary>
		/// Fills image with 
		/// </summary>
		/// <param name="seed"></param>
		/// <param name="monochrome"></param>
		public void Fill ( Color color )
		{
			PerpixelProcessing( p => color );
		}


		/// <summary>
		/// Fills image with 
		/// </summary>
		/// <param name="seed"></param>
		/// <param name="monochrome"></param>
		public void Tint ( Color color )
		{
			PerpixelProcessing( p => p * color );
		}


		/// <summary>
		/// Samples image at given coordinates with wraping addressing mode
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public Color Sample ( int x, int y )
		{
			x = Clamp(x, 0, Width);
			y = Clamp(y, 0, Height);

			var addr = ComputeByteAddress(x,y);

			var r	 = RawImageData[ addr + 0 ];
			var g	 = RawImageData[ addr + 1 ];
			var b	 = RawImageData[ addr + 2 ];
			var a	 = RawImageData[ addr + 3 ];

			return new Color(r,g,b,a);
		}



		/// <summary>
		/// Samples image at given coordinates with wraping addressing mode
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public Color Sample ( float x, float y )
		{
			var	tx	=	Frac( x * Width );
			var	ty	=	Frac( y * Height );
			int	x0	=	Wrap( (int)(x * Width)		, Width );
			int	x1	=	Wrap( (int)(x * Width + 1)	, Width );
			int	y0	=	Wrap( (int)(y * Height)		, Height );
			int	y1	=	Wrap( (int)(y * Height + 1) , Height );
			
			//   xy
			var v00	=	Sample( x0, y0 );
			var v01	=	Sample( x0, y1 );
			var v10	=	Sample( x1, y0 );
			var v11	=	Sample( x1, y1 );

			var v0x	=	Color.Lerp( v00, v01, ty );
			var v1x	=	Color.Lerp( v10, v11, ty );
			return		Color.Lerp( v0x, v1x, tx );
		}



		/// <summary>
		/// Samples image at given coordinates with wraping addressing mode
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public Color SampleMip ( int x, int y, bool wrap = true)
		{
			var c00 = Sample( x*2+0, y*2+0 );
			var c01 = Sample( x*2+0, y*2+1 );
			var c10 = Sample( x*2+1, y*2+0 );
			var c11 = Sample( x*2+1, y*2+1 );

			var c0x	= Color.Lerp( c00, c01, 0.5f );
			var c1x	= Color.Lerp( c10, c11, 0.5f );

			return Color.Lerp( c0x, c1x, 0.5f );
		}



		/// <summary>
		/// Samples average of four neighbouring texels with given top-left corener with clamp addressing mode
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public Color SampleQ4Clamp ( int x, int y )
		{
			var c00 = Sample( x+0, y+0 );
			var c01 = Sample( x+0, y+1 );
			var c10 = Sample( x+1, y+0 );
			var c11 = Sample( x+1, y+1 );

			var c0x	= Color.Lerp( c00, c01, 0.5f );
			var c1x	= Color.Lerp( c10, c11, 0.5f );

			return Color.Lerp( c0x, c1x, 0.5f );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="img"></param>
		public void Copy ( int offsetX, int offsetY, Image img )
		{
			for (int x=0; x<img.Width; x++) {
				for (int y=0; y<img.Height; y++) {
					SetPixel( offsetX + x, offsetY + y, img.Sample( x, y ) );
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="img"></param>
		public void CopySubImageTo ( int srcX, int srcY, int srcWidth, int srcHeight, int dstX, int dstY, Image destination )
		{
			for (int x=0; x<srcWidth; x++) {
				for (int y=0; y<srcHeight; y++) {
					destination.SetPixel( dstX + x, dstY + y, Sample( srcX+x, srcY+y ) );
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="w"></param>
		/// <param name="h"></param>
		/// <param name="color"></param>
		public void DrawRectangle ( int x, int y, int w, int h, Color color )
		{
			for (var i=x; i<x+w; i++) {
				for (var j=y; j<y+h; j++) {
					SetPixel( i,j, color );	
				}
			}
		}



		/// <summary>
		/// Create half-sized image using bilinear filtering
		/// </summary>
		/// <returns></returns>
		public Image DownsampleBilinear ()
		{
			var image = new Image( Width/2, Height/2 );

			image.PerpixelProcessing( (x,y,c) => this.SampleMip( x,y ) );

			return image;
		}



		/// <summary>
		/// Create half-sized image using bilinear filtering
		/// </summary>
		/// <returns></returns>
		public Image DownsampleBilinear (int newWidth, int newHeight)
		{
			var image = new Image( newWidth, newHeight );

			for (int x=0; x<newWidth; x++) 
			{
				for (int y=0; y<newHeight; y++)
				{
					var fx = x / (float)newWidth;
					var fy = y / (float)newHeight;
					image.SetPixel(x, y, Sample(fx, fy));
				}
			}

			return image;
		}



		public void SetPixel ( int x, int y, Color value )
		{
			int addr = ComputeByteAddress( x, y );

			RawImageData[ addr + 0 ]	=	value.R;
			RawImageData[ addr + 1 ]	=	value.G;
			RawImageData[ addr + 2 ]	=	value.B;
			RawImageData[ addr + 3 ]	=	value.A;
		}


		public void SetPixel ( int x, int y, byte gray )
		{
			int addr = ComputeByteAddress( x, y );

			RawImageData[ addr + 0 ]	=	gray;
			RawImageData[ addr + 1 ]	=	gray;
			RawImageData[ addr + 2 ]	=	gray;
			RawImageData[ addr + 3 ]	=	255;
		}


		public void SetPixel ( int x, int y, byte r, byte g, byte b, byte a )
		{
			int addr = ComputeByteAddress( x, y );

			RawImageData[ addr + 0 ]	=	r;
			RawImageData[ addr + 1 ]	=	g;
			RawImageData[ addr + 2 ]	=	b;
			RawImageData[ addr + 3 ]	=	a;
		}


		public void SetPixel ( int x, int y, byte r, byte g, byte b )
		{
			int addr = ComputeByteAddress( x, y );

			RawImageData[ addr + 0 ]	=	r;
			RawImageData[ addr + 1 ]	=	g;
			RawImageData[ addr + 2 ]	=	b;
			RawImageData[ addr + 3 ]	=	255;
		}


		public void SetPixelLinear( int pixelIndex, Color color )
		{
			RawImageData[ pixelIndex * 4 + 0 ] = color.R;
			RawImageData[ pixelIndex * 4 + 1 ] = color.G;
			RawImageData[ pixelIndex * 4 + 2 ] = color.B;
			RawImageData[ pixelIndex * 4 + 3 ] = color.A;
		}


		public Color GetPixelLinear( int pixelIndex )
		{
			var r = RawImageData[ pixelIndex * 4 + 0 ];
			var g = RawImageData[ pixelIndex * 4 + 1 ];
			var b = RawImageData[ pixelIndex * 4 + 2 ];
			var a = RawImageData[ pixelIndex * 4 + 3 ];
			return new Color(r,g,b,a);
		}


		/// <summary>
		/// Does perpixel processing with given function
		/// </summary>
		/// <param name="procFunc"></param>
		public void PerpixelProcessing ( Func<Color, Color> procFunc )
		{
			for (int i=0; i<PixelCount; i++) {
				SetPixelLinear( i, procFunc( GetPixelLinear(i) ) );
			}
		}

		
		/// <summary>
		/// Does perpixel processing with given function
		/// </summary>
		/// <param name="procFunc"></param>
		public void PerpixelProcessing ( Func<int, int, Color, Color> procFunc )
		{
			for (int x=0; x<Width; x++) 
			for (int y=0; y<Height; y++)  
				SetPixel(x,y, procFunc( x, y, Sample( x,y ) ) );
		}



		public Color ComputeAverageColor ()
		{
			Color4 average = Color4.Zero;

			for (int x=0; x<Width; x++) {
				for (int y=0; y<Height; y++) {

					var c = Sample(x,y);

					average.Red		+= c.R;
					average.Green	+= c.G;
					average.Blue	+= c.B;
					average.Alpha	+= c.A;
				}
			}

			average.Red		/= (RawImageData.Length * 255.0f);
			average.Green	/= (RawImageData.Length * 255.0f);
			average.Blue	/= (RawImageData.Length * 255.0f);
			average.Alpha	/= (RawImageData.Length * 255.0f);

			return new Color( average.Red, average.Green, average.Blue, average.Alpha );
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
	}

}
