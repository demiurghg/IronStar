using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core.Mathematics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Fusion.Core.Mathematics;
using Color = Fusion.Core.Mathematics.Color;

namespace Fusion.Engine.Imaging 
{
	public static class ImageLib
	{
		/*-----------------------------------------------------------------------------------------
		 *	Sampling
		-----------------------------------------------------------------------------------------*/

		static public Color SampleQ4( Image<Color> image, int x, int y )
		{
			var c00 = image.GetPixel( x+0, y+0 );
			var c01 = image.GetPixel( x+0, y+1 );
			var c10 = image.GetPixel( x+1, y+0 );
			var c11 = image.GetPixel( x+1, y+1 );

			var c0x	= Color.Lerp( c00, c01, 0.5f );
			var c1x	= Color.Lerp( c10, c11, 0.5f );

			return Color.Lerp( c0x, c1x, 0.5f );
		}


		static public Color AverageFourSamples( Color c00, Color c01, Color c10, Color c11 )
		{
			var c0x	= Color.Lerp( c00, c01, 0.5f );
			var c1x	= Color.Lerp( c10, c11, 0.5f );

			return Color.Lerp( c0x, c1x, 0.5f );
		}


		public static Image<Color> Downsample( Image<Color> srcColor, int newWidth, int newHeight )
		{
			Image<Color> tempImage = srcColor;

			while ( tempImage.Width > newWidth * 2 || tempImage.Height > newHeight * 2 )
			{
				tempImage = tempImage.GenerateMipLevel( AverageFourSamples );
			}

			var outputImage = new Image<Color>( newWidth, newHeight );

			for (int x=0; x<newWidth; x++) 
			{
				for (int y=0; y<newHeight; y++)
				{
					var fx = x / (float)newWidth;
					var fy = y / (float)newHeight;
					outputImage.SetPixel(x, y, tempImage.SampleLinearClamp(fx, fy, Color.Lerp));
				}
			}

			return outputImage;
		}


		public static void SetAlpha ( Image<Color> image, byte alpha )
		{
			image.ForEachPixel( color => new Color( color.R, color.G, color.B, (byte)255 ) );
		}


		public static void SetAlpha ( Image<Color4> image, float alpha )
		{
			image.ForEachPixel( color => new Color4( color.Red, color.Green, color.Blue, alpha ) );
		}


		public static Color ComputeAverageColor ( Image<Color> image )
		{
			Color4 average = Color4.Zero;

			for (int x=0; x<image.Width; x++) {
				for (int y=0; y<image.Height; y++) {

					var c = image.GetPixel(x,y);

					average.Red		+= c.R;
					average.Green	+= c.G;
					average.Blue	+= c.B;
					average.Alpha	+= c.A;
				}
			}

			average.Red		/= (image.RawImageData.Length * 255.0f);
			average.Green	/= (image.RawImageData.Length * 255.0f);
			average.Blue	/= (image.RawImageData.Length * 255.0f);
			average.Alpha	/= (image.RawImageData.Length * 255.0f);

			return new Color( average.Red, average.Green, average.Blue, average.Alpha );
		}

		/*-----------------------------------------------------------------------------------------
		 *	TGA Loading/Saving
		-----------------------------------------------------------------------------------------*/

		[StructLayout(LayoutKind.Sequential,Pack=1)]
		public struct TgaHeader {
		   public byte	idlength;
		   public byte	colourmaptype;
		   public byte	datatypecode;
		   public short	colourmaporigin;
		   public short	colourmaplength;
		   public byte	colourmapdepth;
		   public short	x_origin;
		   public short	y_origin;
		   public short	width;
		   public short	height;
		   public byte	bitsperpixel;
		   public byte	imagedescriptor;
		}


		public static void SaveTga ( Image<Color> image, Stream stream )
		{
			using (var bw = new BinaryWriter(stream)) {

				bw.Write((byte)0);										  /* idlength;			*/
				bw.Write( (byte)0 );									  /* colourmaptype;		*/
				bw.Write( (byte)2 );									  /* datatypecode;		*/
				bw.Write( (byte)0 ); bw.Write( (byte)0 ); 				  /* colourmaporigin;	*/
				bw.Write( (byte)0 ); bw.Write( (byte)0 ); 				  /* colourmaplength;	*/
				bw.Write( (byte)0 );									  /* colourmapdepth;	*/
				bw.Write( (byte)0 ); bw.Write( (byte)0 ); 				  /* x_origin;			*/
				bw.Write( (byte)0 ); bw.Write( (byte)0 ); 				  /* y_origin;			*/
				bw.Write( (byte)( image.Width  & 0x00FF) );				  /* width;				*/
				bw.Write( (byte)((image.Width  & 0xFF00) >> 8) );		  /* -					*/
				bw.Write( (byte)( image.Height & 0x00FF) );				  /* height;			*/
				bw.Write( (byte)((image.Height & 0xFF00) >> 8) );		  /* -					*/
				bw.Write( (byte)32   );									  /* bitsperpixel;		*/
				bw.Write( (byte)0x28 );	/* 0010 1000 */					  /* imagedescriptor;	*/

				for (int y=0; y<image.Height; y++)
				{
					for (int x=0; x<image.Width; x++)
					{
						var c = image.GetPixel( x, y );
						bw.Write(c.B);
						bw.Write(c.G);
						bw.Write(c.R);
						bw.Write(c.A);
					}
				} 
			}
		}


		public static void SaveTga ( Image<Color> image, string path )
		{
			using ( var fs = File.Open( path, FileMode.Create ) ) {
				SaveTga( image, fs );
			}
   		}


		public static TgaHeader TakeTga ( Stream stream )
		{
			using (var br = new BinaryReader( stream )) {

				var header	=	new TgaHeader();

				/* char	 */	header.idlength			=	br.ReadByte();
				/* char	 */	header.colourmaptype	=	br.ReadByte();
				/* char	 */	header.datatypecode		=	br.ReadByte();
				/* short */	header.colourmaporigin	=	br.ReadInt16();
				/* short */	header.colourmaplength	=	br.ReadInt16();
				/* char	 */	header.colourmapdepth	=	br.ReadByte();
				/* short */	header.x_origin			=	br.ReadInt16();
				/* short */	header.y_origin			=	br.ReadInt16();
				/* short */	header.width			=	br.ReadInt16();
				/* short */	header.height			=	br.ReadInt16();
				/* char	 */	header.bitsperpixel		=	br.ReadByte();
				/* char	 */	header.imagedescriptor	=	br.ReadByte();

				if ( header.datatypecode != 2 ) {
					throw new Exception(string.Format("Only uncompressed RGB and RGBA images are supported. Got {0} data type code", header.datatypecode));
				}

				if ( header.bitsperpixel != 24 && header.bitsperpixel != 32 ) {
					throw new Exception(string.Format("Only 24- and 32-bit images are supported. Got {0} bits per pixel", header.bitsperpixel));
				}

				return header;
			}
		}


		public static Image<Color> LoadTga ( Stream stream )
		{
			using (var br = new BinaryReader( stream )) {
				
				TgaHeader header  = new TgaHeader();

				/* char	 */	header.idlength			=	br.ReadByte();
				/* char	 */	header.colourmaptype	=	br.ReadByte();
				/* char	 */	header.datatypecode		=	br.ReadByte();
				/* short */	header.colourmaporigin	=	br.ReadInt16();
				/* short */	header.colourmaplength	=	br.ReadInt16();
				/* char	 */	header.colourmapdepth	=	br.ReadByte();
				/* short */	header.x_origin			=	br.ReadInt16();
				/* short */	header.y_origin			=	br.ReadInt16();
				/* short */	header.width			=	br.ReadInt16();
				/* short */	header.height			=	br.ReadInt16();
				/* char	 */	header.bitsperpixel		=	br.ReadByte();
				/* char	 */	header.imagedescriptor	=	br.ReadByte();

				if ( header.datatypecode != 2 && header.datatypecode != 3 ) {
					throw new Exception(string.Format("Only uncompressed RGB, RGBA anf Grayscale images are supported. Got {0} data type code", header.datatypecode));
				}

				if ( header.bitsperpixel != 24 && header.bitsperpixel != 32 && header.bitsperpixel != 8 ) {
					throw new Exception(string.Format("Only 8, 24 and 32-bit images are supported. Got {0} bits per pixel", header.bitsperpixel));
				}
			
				int w = header.width;
				int h = header.height;
				int bytePerPixel = header.bitsperpixel / 8;

				var	image	= new Image<Color>( w, h );
				var	data	= new byte[ w * h * bytePerPixel ];

				//	skip ID :
				br.ReadBytes( header.idlength );

				//	read image data :
				br.Read( data, 0, w * h * bytePerPixel );

				bool flip	=	!MathUtil.IsBitSet(header.imagedescriptor, 5);

				unsafe {
					if ( bytePerPixel==3 ) 
					{
						for ( int x=0; x<w; ++x ) 
						{
							for ( int y=0; y<h; ++y ) 
							{
								int p =  flip ? ((h-y-1) * w + x) : (y * w + x);

								image.SetPixel( x, y, new Color( data[p*3+2], data[p*3+1], data[p*3+0], (byte)255 ) );
							}
						}
					} 
					else if (bytePerPixel==4) 
					{
						for ( int x=0; x<w; ++x ) 
						{
							for ( int y=0; y<h; ++y ) 
							{
								int p =  flip ? ((h-y-1) * w + x) : (y * w + x);

								image.SetPixel( x, y, new Color( data[p*4+2], data[p*4+1], data[p*4+0], data[p*4+3] ) );
							}
						}
					} 
					else if (bytePerPixel==1) 
					{
						for ( int x=0; x<w; ++x ) 
						{
							for ( int y=0; y<h; ++y ) 
							{
								int p =  flip ? ((h-y-1) * w + x) : (y * w + x);

								image.SetPixel( x, y, new Color( data[p], data[p], data[p], (byte)255 ) );
							}
						}
					}
				}

				return image;
			}
		}


		/*-----------------------------------------------------------------------------------------
		 *	JPEG Loading/Saving
		-----------------------------------------------------------------------------------------*/

		public static Size2 TakeJpgSize( Stream stream )
		{
			var decoder = new JpegBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat|BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
			var bitmapSource = decoder.Frames[0];
			return new Size2( bitmapSource.PixelWidth, bitmapSource.PixelHeight );
		}



		public static Image<Color> LoadJpg ( Stream stream )
		{
			var decoder = new JpegBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
			var bitmapSource = decoder.Frames[0];

			
			var bpp			=	bitmapSource.Format.BitsPerPixel;
			var format		=	bitmapSource.Format;
			var pixelCount  =   bitmapSource.PixelWidth * bitmapSource.PixelHeight;
			var byteCount	=	MathUtil.IntDivRoundUp( pixelCount * bpp, 8 );
			var stride		=	MathUtil.IntDivRoundUp( bitmapSource.PixelWidth * bpp, 8 );
			var pixels		=	new byte[ byteCount ];

			bitmapSource.CopyPixels( Int32Rect.Empty, pixels, stride, 0 );


			var image   =   new Image<Color>( bitmapSource.PixelWidth, bitmapSource.PixelHeight, Color.Black );


			if (format==PixelFormats.Bgr24) 
			{
				for ( int i = 0; i<pixelCount; i++ ) 
				{
					var offset	=	i * 3;
					var color	=	new Color( pixels[offset+2], pixels[offset+1], pixels[offset+0] );
					image.SetPixelLinear( i, color );
				}
			} 
			else if ( format==PixelFormats.Bgra32 ) 
			{
				for ( int i = 0; i<pixelCount; i++ ) 
				{
					var offset  =   i * 4;
					var color   =   new Color( pixels[offset+2], pixels[offset+1], pixels[offset+0], pixels[offset+3] );
					image.SetPixelLinear( i, color );
				}
			} 
			else if ( format==PixelFormats.Gray8 ) 
			{
				for ( int i = 0; i<pixelCount; i++ ) 
				{
					var offset  =   i * 1;
					var color   =   new Color( pixels[offset], pixels[offset], pixels[offset], (byte)255 );
					image.SetPixelLinear( i, color );
				}
			} 
			else 
			{
				throw new NotSupportedException( string.Format("JPG format {0} is not supported", format) );
			}

			return image;
		}


		/*-----------------------------------------------------------------------------------------
		 *	PNG Loading/Saving
		-----------------------------------------------------------------------------------------*/

		public static Size2 TakePngSize( Stream stream )
		{
			PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat|BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
			BitmapSource bitmapSource = decoder.Frames[0];
			return new Size2( bitmapSource.PixelWidth, bitmapSource.PixelHeight );
		}


		public static Image<Color> LoadPng ( Stream stream )
		{
			PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
			BitmapSource bitmapSource = decoder.Frames[0];

			
			var bpp			=	bitmapSource.Format.BitsPerPixel;
			var format		=	bitmapSource.Format;
			var pixelCount  =   bitmapSource.PixelWidth * bitmapSource.PixelHeight;
			var byteCount	=	MathUtil.IntDivRoundUp( pixelCount * bpp, 8 );
			var stride		=	MathUtil.IntDivRoundUp( bitmapSource.PixelWidth * bpp, 8 );
			var pixels		=	new byte[ byteCount ];

			bitmapSource.CopyPixels( Int32Rect.Empty, pixels, stride, 0 );


			var image   =   new Image<Color>( bitmapSource.PixelWidth, bitmapSource.PixelHeight, Color.Black );


			if (format==PixelFormats.Bgr24) 
			{
				for ( int i = 0; i<pixelCount; i++ ) 
				{
					var offset	=	i * 3;
					var color	=	new Color( pixels[offset+2], pixels[offset+1], pixels[offset+0] );
					image.SetPixelLinear( i, color );
				}
			} 
			else if ( format==PixelFormats.Bgra32 ) 
			{
				for ( int i = 0; i<pixelCount; i++ ) 
				{
					var offset  =   i * 4;
					var color   =   new Color( pixels[offset+2], pixels[offset+1], pixels[offset+0], pixels[offset+3] );
					image.SetPixelLinear( i, color );
				}
			} 
			else if ( format==PixelFormats.Gray8 ) 
			{
				for ( int i = 0; i<pixelCount; i++ ) 
				{
					var offset  =   i * 1;
					var color   =   new Color( pixels[offset], pixels[offset], pixels[offset], (byte)255 );
					image.SetPixelLinear( i, color );
				}													  
			} 
			else if ( format==PixelFormats.Indexed8 ) 
			{
				for ( int i = 0; i<pixelCount; i++ ) 
				{
					var offset  =   i * 1;
					var color   =   new Color( pixels[offset], pixels[offset], pixels[offset], (byte)255 );
					image.SetPixelLinear( i, color );
				}
			} 
			else 
			{
				throw new NotSupportedException( string.Format("PNG format {0} is not supported", format) );
			}

			return image;
		}
	}
}
