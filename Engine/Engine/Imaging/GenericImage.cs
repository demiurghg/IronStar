using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core.Mathematics;
using System.Runtime.InteropServices;

namespace Fusion.Engine.Imaging 
{
	public partial class GenericImage<TColor> 
	{

		readonly int width;
		readonly int height;
		readonly int pixelSize;
		readonly byte[] rawImageData;

		public int		Width	{ get { return width; } }
		public int		Height	{ get { return height; } }
		public byte[]	RawImageData { get { return rawImageData; } }

		public object Tag { get; set; }

		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="width">Image width</param>
		/// <param name="height">Image height</param>
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
			rawImageData	=	AllocRawImage( width, height, out pixelSize );
		}



		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="width">Image width</param>
		/// <param name="height">Image height</param>
		/// <param name="fillColor">Color to fill image</param>
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
			rawImageData	=	AllocRawImage( width, height, out pixelSize );

			Fill( fillColor );
		}



		byte[] AllocRawImage( int width, int height, out int pixelSize )
		{
			pixelSize	=	Marshal.SizeOf(typeof(TColor));
			int length	=	width * height * pixelSize;
			return new byte[length];
		}



		/// <summary>
		/// Returns address of pixel with given coordinates and adressing mode
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <param name="wrap"></param>
		/// <returns></returns>
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
		public TColor GetPixel ( int x, int y, bool wrap = true)
		{
			unsafe 
			{
				int addr = GetByteAddress( x, y );
				fixed (byte *ptr = &rawImageData[addr])
				{
					return (TColor)Marshal.PtrToStructure( new IntPtr(ptr), typeof(TColor) );
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
		public void SetPixel ( int x, int y, TColor value, bool wrap = true )
		{
			unsafe 
			{
				int addr = GetByteAddress( x, y );
				fixed (byte *ptr = &rawImageData[addr])
				{
					Marshal.StructureToPtr( value, new IntPtr(ptr), true );
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
				return GetPixel(xy.X, xy.Y, false);
			}
			
			set {
				SetPixel(xy.X, xy.Y, value, false);
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
				return GetPixel(x, y, false);
			}
			
			set {
				SetPixel(x,y, value, false);
			}	

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

	}

}
