using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core.Mathematics;


namespace Fusion.Engine.Imaging {
	public partial class GenericVolume<TColor> {

		public int	Width	{ get; protected set; }
		public int	Height	{ get; protected set; }
		public int	Depth	{ get; protected set; }

		public TColor[]	RawImageData { get; protected set; }

		public object Tag { get; set; }
		

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="width">Image width</param>
		/// <param name="height">Image height</param>
		public GenericVolume ( int width, int height, int depth )
		{
			RawImageData	=	new TColor[width*height*depth];

			Width	=	width;
			Height	=	height;
			Depth	=	depth;

			if (Width<=0) {
				throw new ArgumentOutOfRangeException("Volume width must be > 0");
			}

			if (Height<=0) {
				throw new ArgumentOutOfRangeException("Volume height must be > 0");
			}

			if (Depth<=0) {
				throw new ArgumentOutOfRangeException("Volume depth must be > 0");
			}
		}



		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="width">Image width</param>
		/// <param name="height">Image height</param>
		/// <param name="fillColor">Color to fill image</param>
		public GenericVolume ( int width, int height, int depth, TColor fillColor )
		{
			RawImageData	=	new TColor[width*height*depth];

			Width	=	width;
			Height	=	height;
			Depth	=	depth;

			if (Width<=0) {
				throw new ArgumentOutOfRangeException("Volume width must be > 0");
			}

			if (Height<=0) {
				throw new ArgumentOutOfRangeException("Volume height must be > 0");
			}

			if (Depth<=0) {
				throw new ArgumentOutOfRangeException("Volume depth must be > 0");
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
		public int Address ( int x, int y, int z )
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
				return Sample(x, y, z);
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
				return Sample(xyz.X, xyz.Y, xyz.Z);
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
		public TColor Sample ( int x, int y, int z )
		{
			return RawImageData[ Address( x, y, z ) ];
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="img"></param>
		public void CopyTo ( GenericVolume<TColor> destination )
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
			RawImageData[ Address( x, y, z ) ] = value;
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

	}

}
