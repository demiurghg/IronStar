using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core.Mathematics;
using System.Runtime.InteropServices;
using Fusion.Core.Utils;
using SharpDX;

namespace Fusion.Engine.Imaging 
{
	public partial class GenericImage<TColor> where TColor: struct 
	{
		public static readonly uint FourCC	= 0x494d4730; // IMG0
		public static readonly uint TypeCrc;
		
		static GenericImage()
		{
			TypeCrc	=	Crc32.ComputeChecksum( Encoding.UTF8.GetBytes(typeof(TColor).ToString()));
		}
		
		public delegate TColor MipGenFunc(TColor c00, TColor c01, TColor c10, TColor c11);
		public delegate TColor LerpFunc(TColor c0, TColor c1, float t);

		readonly int width;
		readonly int height;
		readonly int pixelSize;
		readonly byte[] rawImageData;
		readonly Int3[] mipDimensions;
		readonly DataStream stream;
		readonly bool isColor;

		public int		Width	{ get { return width; } }
		public int		Height	{ get { return height; } }
		public byte[]	RawImageData { get { return rawImageData; } }
		public int		PixelCount { get { return width * height; } }

		public object Tag { get; set; }


		

		public GenericImage ( int width, int height )
		{
			if (width<=0) {
				throw new ArgumentOutOfRangeException("Image width must be > 0");
			}

			if (height<=0) {
				throw new ArgumentOutOfRangeException("Image height must be > 0");
			}

			this.width		=	width;
			this.height		=	height;
			this.isColor	=	typeof(TColor) == typeof(Color);
			rawImageData	=	AllocRawImage( out pixelSize );
		}



		public GenericImage ( int width, int height, TColor fillColor )
		{
			if (width<=0) {
				throw new ArgumentOutOfRangeException("Image width must be > 0");
			}

			if (height<=0) {
				throw new ArgumentOutOfRangeException("Image height must be > 0");
			}

			this.width		=	width;
			this.height		=	height;
			this.isColor	=	typeof(TColor) == typeof(Color);
			rawImageData	=	AllocRawImage( out pixelSize );

			Fill( fillColor );
		}



		byte[] AllocRawImage( out int pixelSize )
		{
			pixelSize	=	Marshal.SizeOf(typeof(TColor));
			int length	=	width * height * pixelSize;

			var data	=	new byte[length];

			return data;
		}



		int GetByteAddress ( int x, int y )
		{
			if (x<0 || x>=width ) throw new ArgumentOutOfRangeException(string.Format("X = {0} is out of range [0, {1})", x, width));
			if (y<0 || y>=height) throw new ArgumentOutOfRangeException(string.Format("Y = {0} is out of range [0, {1})", y, height));

			return ( x + y * Width ) * pixelSize;
		}


		/// <summary>
		/// Gets pixel at given coordinates
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public TColor GetPixel ( int x, int y )
		{
			unsafe 
			{
				fixed (byte *ptr = &rawImageData[ GetByteAddress( x, y ) ])
				{
					return Utilities.Read<TColor>( new IntPtr(ptr) );
				}
			}
		}



		/// <summary>
		/// Sets pixel at given coordinates
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <param name="color"></param>
		/// <param name="wrap"></param>
		public void SetPixel ( int x, int y, TColor value )
		{
			unsafe 
			{
				fixed (byte *ptr = &rawImageData[ GetByteAddress( x, y ) ])
				{
					Utilities.Write<TColor>( new IntPtr(ptr), ref value );
				}
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
				return GetPixel(xy.X, xy.Y);
			}
			
			set {
				SetPixel(xy.X, xy.Y, value);
			}	

		}


		/// <summary>
		/// Sets and gets pixel color at given coordinates.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public TColor this[int x, int y] {
			get {
				return GetPixel(x, y);
			}
			
			set {
				SetPixel(x,y, value);
			}	

		}


		public void SetPixelLinear( int index, TColor color )
		{
			SetPixel( index % Width, index / Height, color );
		}


		public TColor GetPixelLinear( int index )
		{
			return GetPixel( index % Width, index / Height );
		}

		/*------------------------------------------------------------------------------------------
		 *	Simple image processing
		 *	More complex image processing and drawing must be placed in separate classes.
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// Converts image to another image format
		/// </summary>
		/// <typeparam name="TOutputColor"></typeparam>
		/// <param name="convert"></param>
		/// <returns></returns>
		public GenericImage<TOutputColor> Convert<TOutputColor>( Func<TColor, TOutputColor> convert ) where TOutputColor: struct
		{
			var outputImage = new GenericImage<TOutputColor>( Width, Height );

			for (int x=0; x<Width; x++)
			{
				for (int y=0; y<Height; y++)
				{
					outputImage.SetPixel( x, y, convert( GetPixel( x, y ) ) );
				}
			}

			return outputImage;
		}


		/// <summary>
		/// Copies give image to another image. 
		/// If target image is smaller than original, bottom right part of the image will be cropped.
		/// </summary>
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
		/// Copy subimage of the given image to another image in provided coordinates.
		/// </summary>
		public void CopySubImageTo ( int srcX, int srcY, int srcWidth, int srcHeight, int dstX, int dstY, GenericImage<TColor> destination )
		{
			#warning add boundry checks!
			for (int x=0; x<srcWidth; x++) 
			{
				for (int y=0; y<srcHeight; y++) 
				{
					destination.SetPixel( dstX + x, dstY + y, GetPixel( srcX+x, srcY+y ) );
				}
			}
		}


		/// <summary>
		/// Fills entire image with given color
		/// </summary>
		public void Fill ( TColor color )
		{
			for (int i=0; i<width; i++)
			{
				for (int j=0; j<height; j++)
				{
					this[i,j] = color;
				}
			}
		}


		/// <summary>
		/// Fills provided image rectangle with given color
		/// </summary>
		public void FillRect ( Rectangle rect, TColor color )
		{
			#warning add boundry checks!
			for (int i=rect.Left; i<rect.Right; i++)
			{
				for (int j=rect.Top; j<rect.Bottom; j++)
				{
					this[i,j] = color;
				}
			}
		}


		/// <summary>
		/// Does perpixel processing with given function
		/// </summary>
		/// <param name="procFunc"></param>
		public void PerpixelProcessing ( Func<int, int, TColor, TColor> procFunc )
		{
			for (int x=0; x<Width; x++) 
			{
				for (int y=0; y<Height; y++)  
				{
					SetPixel(x,y, procFunc( x, y, GetPixel( x,y ) ) );
				}
			}
		}


		/// <summary>
		/// Does perpixel processing with given function
		/// </summary>
		/// <param name="procFunc"></param>
		public void PerpixelProcessing ( Func<TColor, TColor> procFunc )
		{
			for (int x=0; x<Width; x++) 
			{
				for (int y=0; y<Height; y++)  
				{
					SetPixel(x,y, procFunc( GetPixel( x,y ) ) );
				}
			}
		}


		/*------------------------------------------------------------------------------------------
		 *	Sampling :
		-----------------------------------------------------------------------------------------*/

		public static int Clamp ( int x, int min, int max ) 
		{
			if (x <  min) return min;
			if (x >= max) return max-1;
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


		public TColor SampleLinearClamp( float x, float y, LerpFunc lerpFunc )
		{
			var	tx	=	Frac( x * Width );
			var	ty	=	Frac( y * Height );
			int	x0	=	Clamp( (int)(x * Width)			, 0, Width );
			int	x1	=	Clamp( (int)(x * Width + 1)		, 0, Width );
			int	y0	=	Clamp( (int)(y * Height)		, 0, Height );
			int	y1	=	Clamp( (int)(y * Height + 1)	, 0, Height );
			
			//   xy
			var v00	=	GetPixel( x0, y0 );
			var v01	=	GetPixel( x0, y1 );
			var v10	=	GetPixel( x1, y0 );
			var v11	=	GetPixel( x1, y1 );

			var v0x	=	lerpFunc( v00, v01, ty );
			var v1x	=	lerpFunc( v10, v11, ty );
			return		lerpFunc( v0x, v1x, tx );
		}


		/*------------------------------------------------------------------------------------------
		 *	Mip generator
		-----------------------------------------------------------------------------------------*/

		public void GenerateMipLevel( GenericImage<TColor> targetImage, MipGenFunc mipGenFunc  )
		{
			var dstWidth  = Math.Min( targetImage.Width,  Width  / 2 );
			var dstHeight = Math.Min( targetImage.Height, Height / 2 );

			for (int x=0; x<dstWidth; x++)
			{
				for (int y=0; y<dstHeight; y++)
				{
					var c00	=	GetPixel( x*2+0, y*2+0 );
					var c01	=	GetPixel( x*2+0, y*2+1 );
					var c10	=	GetPixel( x*2+1, y*2+0 );
					var c11	=	GetPixel( x*2+1, y*2+1 );

					targetImage.SetPixel( x, y, mipGenFunc( c00, c01, c10, c11 ) );
				}
			}
		}


		public GenericImage<TColor> GenerateMipLevel( MipGenFunc mipGenFunc  )
		{
			var mipImage = new GenericImage<TColor>( width / 2, height / 2 );

			GenerateMipLevel( mipImage, mipGenFunc );

			return mipImage;
		}
	

		/*------------------------------------------------------------------------------------------
		 *	Image I/O
		-----------------------------------------------------------------------------------------*/

		public void WriteStream( Stream stream )
		{
			stream.Write( BitConverter.GetBytes(FourCC)	, 0, 4 );	
			stream.Write( BitConverter.GetBytes(TypeCrc), 0, 4 );	
			stream.Write( BitConverter.GetBytes(Width)	, 0, 4 );	
			stream.Write( BitConverter.GetBytes(Height)	, 0, 4 );	

			stream.Write( RawImageData, 0, RawImageData.Length );
		}


		static void ReadHeader( Stream stream, out int width, out int height )
		{
			var header	=	new byte[16];
			stream.Read( header, 0, 16 );

			var magic   =   BitConverter.ToUInt32( header,  0 );
			var type    =   BitConverter.ToUInt32( header,  4 );
				width   =   BitConverter.ToInt32 ( header,  8 );
				height  =   BitConverter.ToInt32 ( header, 12 );

			if ( magic  != FourCC ) throw new IOException( "Bad FourCC, IMG0 expected" );
			if ( type   != TypeCrc ) throw new IOException( "Bad type CRC32" );
		}


		public void ReadStream( Stream stream )
		{
			int width, height;
			ReadHeader( stream, out width, out height );

			if ( width  != Width ) throw new IOException( string.Format( "Bad image width {0}, expected {1}", width, Width ) );
			if ( height != Height ) throw new IOException( string.Format( "Bad image рушпре {0}, expected {1}", height, Height ) );

			var read = stream.Read( RawImageData, 0, RawImageData.Length );

			if ( read!=RawImageData.Length ) throw new IOException("Corrupted image");
		}


		static GenericImage<TColor> FromStream( Stream stream )
		{
			int width, height;
			ReadHeader( stream, out width, out height );

			var image	=	new GenericImage<TColor>( width, height );
			var read	=	stream.Read( image.RawImageData, 0, image.RawImageData.Length );

			if ( read!=image.RawImageData.Length ) throw new IOException("Corrupted image");

			return image;
		}

	}

}
