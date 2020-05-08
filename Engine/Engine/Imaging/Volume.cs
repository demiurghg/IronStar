using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core.Mathematics;
using Fusion.Core.Utils;
using Fusion.Core.Content;
using System.Runtime.InteropServices;
using SharpDX;

namespace Fusion.Engine.Imaging {
	public partial class Volume<TColor> where TColor: struct {

		public static readonly uint FourCC;
		public static readonly uint TypeCrc;
		
		static Volume()
		{
			FourCC	=	ContentUtils.MakeFourCC("VXL0");
			TypeCrc	=	Crc32.ComputeChecksum( Encoding.UTF8.GetBytes(typeof(TColor).ToString()));
		}
		
		public int	Width	{ get { return width; } }
		public int	Height	{ get { return height; } }
		public int	Depth	{ get { return depth; } }

		public byte[]	RawImageData { get { return rawImageData; } }

		readonly int	width;
		readonly int	height;
		readonly int	depth;
		readonly byte[] rawImageData;
		readonly int	voxelSize;

		public object Tag { get; set; }
		

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="width">Image width</param>
		/// <param name="height">Image height</param>
		public Volume ( int width, int height, int depth )
		{
			if (width<=0) {
				throw new ArgumentOutOfRangeException("Volume width must be > 0");
			}

			if (height<=0) {
				throw new ArgumentOutOfRangeException("Volume height must be > 0");
			}

			if (depth<=0) {
				throw new ArgumentOutOfRangeException("Volume depth must be > 0");
			}

			this.width		=	width;
			this.height		=	height;
			this.depth		=	depth;

			voxelSize		=	Marshal.SizeOf( typeof(TColor) );
			rawImageData	=	new byte[width*height*depth*voxelSize];
		}



		/// <summary>
		/// Returns address of pixel with given coordinates and adressing mode
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <param name="wrap"></param>
		/// <returns></returns>
		public int GetByteAddress ( int x, int y, int z )
		{
			x	=	Clamp( x, 0, Width - 1 );
			y	=	Clamp( y, 0, Height - 1 );
			z	=	Clamp( z, 0, Depth - 1 );

			return x + y * Width + z * Width * Height;
		}



		/// <summary>
		/// Sets and gets pixel color at given coordinates.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public TColor this[int x, int y, int z] {
			get {
				return GetVoxel(x, y, z);
			}
			
			set {
				SetVoxel(x,y,z, value);
			}	

		}



		/// <summary>
		/// Sets and gets pixel color at given coordinates.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public TColor this[Int3 xyz] {
			get {
				return GetVoxel(xyz.X, xyz.Y, xyz.Z);
			}
			
			set {
				SetVoxel(xyz.X, xyz.Y, xyz.Z, value);
			}	

		}


		/// <summary>
		/// Samples image at given coordinates with wraping addressing mode
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public TColor GetVoxel ( int x, int y, int z )
		{
			unsafe 
			{
				fixed (byte *ptr = &rawImageData[ GetByteAddress( x, y, z ) ])
				{
					return Utilities.Read<TColor>( new IntPtr(ptr) );
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="img"></param>
		public void CopyTo ( Volume<TColor> destination )
		{
			var w = Math.Min( Width, destination.Width );
			var h = Math.Min( Height, destination.Height );
			var d = Math.Min( Depth, destination.Depth );

			for (int x=0; x<w; x++) {
				for (int y=0; y<h; y++) {
					for (int z=0; z<d; z++) {
						destination[x,y,z] = this[x,y,z];
					}
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
		public void SetVoxel ( int x, int y, int z, TColor value )
		{
			unsafe 
			{
				fixed (byte *ptr = &rawImageData[ GetByteAddress( x, y, z ) ])
				{
					Utilities.Write<TColor>( new IntPtr(ptr), ref value );
				}
			}
		}



		public void Fill ( TColor color )
		{
			for (int i=0; i<width; i++)
			{
				for (int j=0; j<height; j++)
				{
					for (int k=0; k<Depth; k++)
					{
						this[i,j,k] = color;
					}
				}
			}
		}
		

		/*-----------------------------------------------------------------------------------------
		 *	Simple Math :
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


		/*------------------------------------------------------------------------------------------
		 *	Image I/O
		-----------------------------------------------------------------------------------------*/

		public void WriteStream( Stream stream )
		{
			stream.Write( BitConverter.GetBytes(FourCC)	, 0, 4 );	
			stream.Write( BitConverter.GetBytes(TypeCrc), 0, 4 );	
			stream.Write( BitConverter.GetBytes(Width)	, 0, 4 );	
			stream.Write( BitConverter.GetBytes(Height)	, 0, 4 );	
			stream.Write( BitConverter.GetBytes(Depth)	, 0, 4 );	

			stream.Write( RawImageData, 0, RawImageData.Length );
		}


		static void ReadHeader( Stream stream, out int width, out int height, out int depth )
		{
			var header	=	new byte[16];
			stream.Read( header, 0, 16 );

			var magic   =   BitConverter.ToUInt32( header,  0 );
			var type    =   BitConverter.ToUInt32( header,  4 );
				width   =   BitConverter.ToInt32 ( header,  8 );
				height  =   BitConverter.ToInt32 ( header, 12 );
				depth	=   BitConverter.ToInt32 ( header, 12 );

			if ( magic  != FourCC ) throw new IOException( "Bad FourCC, IMG0 expected" );
			if ( type   != TypeCrc ) throw new IOException( "Bad type CRC32" );
		}


		public void ReadStream( Stream stream )
		{
			int width, height, depth;
			ReadHeader( stream, out width, out height, out depth );

			if ( width  != Width ) throw new IOException( string.Format( "Bad image width {0}, expected {1}", width, Width ) );
			if ( height != Height ) throw new IOException( string.Format( "Bad image height {0}, expected {1}", height, Height ) );
			if ( height != Height ) throw new IOException( string.Format( "Bad image depth {0}, expected {1}", height, Height ) );

			var read = stream.Read( RawImageData, 0, RawImageData.Length );

			if ( read!=RawImageData.Length ) throw new IOException("Corrupted volume");
		}


		public static Volume<TColor> FromStream( Stream stream )
		{
			int width, height, depth;
			ReadHeader( stream, out width, out height, out depth );

			var image	=	new Volume<TColor>( width, height, depth );
			var read	=	stream.Read( image.RawImageData, 0, image.RawImageData.Length );

			if ( read!=image.RawImageData.Length ) throw new IOException("Corrupted volume");

			return image;
		}
	}

}
