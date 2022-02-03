using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Core.Mathematics;
using Fusion.Engine.Imaging;

namespace Fusion.Build.Mapping {
	
	/// <summary>
	/// https://www.infoworld.com/article/3221392/how-to-use-the-object-pool-design-pattern-in-c.html
	/// </summary>
	public class VTTile 
	{
		static Random rand = new Random();		

		VTAddress	address;

		/// <summary>
		/// Gets address of the tile
		/// </summary>
		public VTAddress VirtualAddress;
		public Rectangle PhysicalAddress;

		Image<Color>	colorData;			//	RGB - base color
		Image<Color>	normalData;			//	RGB - normal
		Image<Color>	specularData;		//	Roughness, Metalness, Emission, Ambient Occlusion

		Image<Color>	colorDataMip;
		Image<Color>	normalDataMip;
		Image<Color>	specularDataMip;


		/// <summary>
		/// Creates instance of VTTile
		/// </summary>
		public VTTile ( VTAddress address )
		{
			this.address	=	address;
			var size		=	VTConfig.PageSizeBordered;
			colorData		=	new Image<Color>	(size, size);
			normalData		=	new Image<Color>	(size, size);
			specularData	=	new Image<Color>	(size, size);
			colorDataMip	=	new Image<Color>	(size/2, size/2);
			normalDataMip	=	new Image<Color>	(size/2, size/2);
			specularDataMip	=	new Image<Color>	(size/2, size/2);

			PhysicalAddress	=	new Rectangle(0,0,0,0);
		}



		/// <summary>
		/// Create instance of tile from three images. Images must be the same size and has equal width and height.
		/// Width and height must be equal VTConfig.PageBorderWidth
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		public VTTile ( VTAddress address, Image<Color> a, Image<Color> b, Image<Color> c )
		{
			this.address	=	address;

			if ( a.Width!=a.Height && a.Width!=VTConfig.PageSizeBordered) {
				throw new ArgumentException("Image width and height must be equal " + VTConfig.PageBorderWidth ); 
			}
			if ( b.Width!=a.Height && b.Width!=VTConfig.PageSizeBordered) {
				throw new ArgumentException("Image width and height must be equal " + VTConfig.PageBorderWidth ); 
			}
			if ( c.Width!=a.Height && c.Width!=VTConfig.PageSizeBordered) {
				throw new ArgumentException("Image width and height must be equal " + VTConfig.PageBorderWidth ); 
			}

			colorData		=	a;
			normalData		=	b;
			specularData	=	c;

			var size		=	VTConfig.PageSizeBordered;

			colorDataMip	=	new Image<Color>	(size/2, size/2);
			normalDataMip	=	new Image<Color>	(size/2, size/2);
			specularDataMip	=	new Image<Color>	(size/2, size/2);
		}



		/// <summary>
		/// Clears tile with particular color, flat normal and no specular.
		/// </summary>
		/// <param name="color"></param>
		public void Clear( Color color )
		{
			colorData.Fill( color );
			normalData.Fill( Color.FlatNormals );
			specularData.Fill( Color.Black );

			colorDataMip?.Fill( color );
			normalDataMip?.Fill( Color.FlatNormals );
			specularDataMip?.Fill( Color.Black );
		}


		/// <summary>
		/// Clears tile with particular color, flat normal and no specular.
		/// </summary>
		/// <param name="color"></param>
		public void MakeWhiteDiffuse()
		{
			colorData.Fill( PbrReference.DielectricMax );
			specularData.ForEachPixel( spec => new Color( (byte)255, (byte)0, (byte)0, spec.A ) );

			colorDataMip?.Fill( PbrReference.DielectricMax );
			specularDataMip?.ForEachPixel( spec => new Color( (byte)255, (byte)0, (byte)0, spec.A ) );
		}


		/// <summary>
		/// Clears tile with particular color, flat normal and no specular.
		/// </summary>
		/// <param name="color"></param>
		public void MakeGlossyMetal()
		{
			colorData.Fill( Color.Gray );
			specularData.ForEachPixel( spec => new Color( spec.R, (byte)255, (byte)0, spec.A ) );

			colorDataMip?.Fill( Color.Gray );
			specularDataMip?.ForEachPixel( spec => new Color( spec.R, (byte)255, (byte)0, spec.A ) );
		}


		/// <summary>
		/// Clears tile with particular color, flat normal and no specular.
		/// </summary>
		/// <param name="color"></param>
		public void MakeMirror()
		{
			byte roughness = MathUtil.Lerp((byte)0, (byte)255, VTSystem.MirrorRoughness );

			colorData.Fill( Color.White );
			specularData.ForEachPixel( spec => new Color( roughness, (byte)255, (byte)0, spec.A ) );

			colorDataMip?.Fill( Color.White );
			specularDataMip?.ForEachPixel( spec => new Color( roughness, (byte)255, (byte)0, spec.A ) );
		}



		/// <summary>
		/// Sampling perfomed using coordinates from top-left corner including border
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		public void SampleQ4( int x, int y, ref Color c, ref Color n, ref Color s )
		{
			//var min	=	VTConfig.PageBorderWidth;
			//var max =	VTConfig.PageSizeBordered - VTConfig.PageBorderWidth;

			//if (x<min) throw new ArgumentException("x < " + min.ToString() );
			//if (y<min) throw new ArgumentException("x < " + min.ToString() );
			//if (x>max) throw new ArgumentException("x > " + max.ToString() );
			//if (y>max) throw new ArgumentException("x > " + max.ToString() );

			c	=	ImageLib.SampleQ4(colorData, x, y );
			n	=	ImageLib.SampleQ4(normalData, x, y );
			s	=	ImageLib.SampleQ4(specularData, x, y );
		}



		/// <summary>
		/// Gets GPU-ready data
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public byte[] GetGpuData(int index, int mip) 
		{
			if (mip==0) {
				switch ( index ) {
					case 0: return colorData.RawImageData;
					case 1: return normalData.RawImageData;
					case 2: return specularData.RawImageData;
					default: return null;
				}
			} else {
				switch ( index ) {
					case 0: return colorDataMip.RawImageData;
					case 1: return normalDataMip.RawImageData;
					case 2: return specularDataMip.RawImageData;
					default: return null;
				}
			}
		}



		/// <summary>
		/// Builds next mip level for given tile
		/// </summary>
		public void GenerateMipLevel ()
		{
			int size = VTConfig.PageSizeBordered/2;

			Color c = Color.Black;
			Color s = Color.Black;
			Color n = Color.Black;
					
			for (int x=0; x<size; x++) {
				for (int y=0; y<size; y++) {

					SampleQ4( x*2, y*2, ref c, ref n, ref s );

					colorDataMip	.SetPixel( x, y, c );
					normalDataMip	.SetPixel( x, y, n );
					specularDataMip	.SetPixel( x, y, s );
				}
			}
		}



		/// <summary>
		/// Sets values.
		/// Addressing is performed from top-left corner including border
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		public void SetValues ( int x, int y, ref Color a, ref Color b, ref Color c )
		{
			colorData.SetPixel( x, y, a );
			normalData.SetPixel( x, y, b );
			specularData.SetPixel( x, y, c );
		}



		/// <summary>
		/// Writes tile data to stream.
		/// </summary>
		/// <param name="stream"></param>
		public void Write ( Stream stream )
		{
			using ( var writer = new BinaryWriter( stream ) ) {
				writer.WriteFourCC( "TILE" );

				writer.Write( colorData.RawImageData );
				writer.Write( normalData.RawImageData );
				writer.Write( specularData.RawImageData );

				writer.Write( colorDataMip.RawImageData );
				writer.Write( normalDataMip.RawImageData );
				writer.Write( specularDataMip.RawImageData );
			}
		}



		public void WriteDebug ( Stream stream )
		{
			ImageLib.SaveTga( colorData, stream );
		}
		


		/// <summary>
		/// Reads tile data from stream.
		/// </summary>
		/// <param name="stream"></param>
		public void Read ( Stream stream )
		{
			using ( var reader = new BinaryReader( stream ) ) {
				if (reader.ReadFourCC()!="TILE") {
					throw new IOException("Bad virtual texture tile format");
				}

				var size		=	VTConfig.PageSizeBordered;

				var length		=	size * size * 4;

				reader.Read( colorData.RawImageData, 0, length ); 
				reader.Read( normalData.RawImageData, 0, length ); 
				reader.Read( specularData.RawImageData, 0, length ); 

				reader.Read( colorDataMip.RawImageData, 0, length/4 );
				reader.Read( normalDataMip.RawImageData, 0, length/4 );
				reader.Read( specularDataMip.RawImageData, 0, length/4 );
			}
		}





		/// <summary>
		/// Draws text
		/// </summary>
		/// <param name="font"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="text"></param>
		public void DrawText ( int x, int y, string text )
		{
			ImageText.DrawText( colorData, x, y, text );
		}



		/// <summary>
		/// Fills tile with random color
		/// </summary>
		public void FillRandomColor ()
		{
			var color = rand.NextColor();

			Clear( color );
		}



		/// <summary>
		/// Draws checkers
		/// </summary>
		public void DrawChecker ()
		{
			int s	=	VTConfig.PageSize;
			var b	=	VTConfig.PageBorderWidth;

			for (int i=-b; i<s+b; i++) {
				for (int j=-b; j<s+b; j++) {
					
					var m = this.VirtualAddress.MipLevel;
					int u = i + b;
					int v = j + b;

					var c = (((i << m ) >> 5) + ((j << m ) >> 5)) & 0x01;

					normalData.SetPixel( u,v, Color.FlatNormals );
					specularData.SetPixel( u,v, Color.Black );

					if (c==0) {
						colorData.SetPixel( u,v, Color.Black );
					} else {
						colorData.SetPixel( u,v, Color.White );
					}
				}			
			}

			GenerateMipLevel();
		}



		Color[] mipColors = new Color[] {
			Color.White,   	Color.Red,		Color.Green,  Color.Blue,
			Color.Yellow,  	Color.Magenta,	Color.Cyan,	  	  
		};


		public void DrawMipLevels ( bool border )
		{
			normalData.Fill( Color.FlatNormals );
			specularData.Fill( Color.Black );

			normalDataMip.Fill( Color.FlatNormals );
			specularDataMip.Fill( Color.Black );

			int count	= mipColors.Length;
			int mip		= address.MipLevel;

			colorData	.ForEachPixel( color => color * mipColors[ (mip+0) % count ] );
			colorDataMip.ForEachPixel( color => color * mipColors[ (mip+1) % count ] );

			if (border) {
				DrawBorder();
				DrawBorderMip();
			}
		}



		/// <summary>
		/// Draws border
		/// </summary>
		public void DrawBorder ()
		{
			int s	=	VTConfig.PageSize;
			var b	=	VTConfig.PageBorderWidth;

			for (int i=b; i<s+b; i++) {
				colorData.SetPixel( b,     i,		Color.Yellow );
				colorData.SetPixel( b+s-1,	i,		Color.Yellow );
				colorData.SetPixel( i,		b,      Color.Yellow );
				colorData.SetPixel( i,		b+s-1,	Color.Yellow );

				/*colorData.SetPixel( b+1,    i,		Color.Yellow );
				colorData.SetPixel( b+s-2,	i,		Color.Yellow );
				colorData.SetPixel( i,		b+1,    Color.Yellow );
				colorData.SetPixel( i,		b+s-2,	Color.Yellow );*/
			}
		}



		/// <summary>
		/// Draws border
		/// </summary>
		public void DrawBorderMip ()
		{
			int s	=	VTConfig.PageSize/2;
			var b	=	VTConfig.PageBorderWidth/2;

			for (int i=b; i<s+b; i++) {
				colorDataMip.SetPixel( b,		i,		Color.Red );
				colorDataMip.SetPixel( b+s-1,	i,		Color.Red );
				colorDataMip.SetPixel( i,		b,      Color.Red );
				colorDataMip.SetPixel( i,		b+s-1,	Color.Red );
			}
		}

	}
}
