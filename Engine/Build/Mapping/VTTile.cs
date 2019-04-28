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
	
	public class VTTile {

		static Random rand = new Random();		

		VTAddress	address;

		/// <summary>
		/// Gets address of the tile
		/// </summary>
		public VTAddress VirtualAddress {
			get {
				return address;
			}
		}

		Image	colorData;
		Image	normalData;
		Image	specularData;

		Image	colorDataMip;
		Image	normalDataMip;
		Image	specularDataMip;

		
		/// <summary>
		/// Creates instance of VTTile
		/// </summary>
		public VTTile ( VTAddress address )
		{
			this.address	=	address;
			var size		=	VTConfig.PageSizeBordered;
			colorData		=	new Image(size, size);
			normalData		=	new Image(size, size);
			specularData	=	new Image(size, size);
			colorDataMip	=	new Image(size/2, size/2);
			normalDataMip	=	new Image(size/2, size/2);
			specularDataMip	=	new Image(size/2, size/2);
		}



		/// <summary>
		/// Create instance of tile from three images. Images must be the same size and has equal width and height.
		/// Width and height must be equal VTConfig.PageBorderWidth
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		public VTTile ( VTAddress address, Image a, Image b, Image c )
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

			colorDataMip	=	new Image(size/2, size/2);
			normalDataMip	=	new Image(size/2, size/2);
			specularDataMip	=	new Image(size/2, size/2);
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
			colorData.Fill( Color.Gray );
			specularData.PerpixelProcessing( spec => new Color( (byte)255, (byte)0, (byte)0, spec.A ) );

			colorDataMip?.Fill( Color.Gray );
			specularDataMip?.PerpixelProcessing( spec => new Color( (byte)255, (byte)0, (byte)0, spec.A ) );
		}


		/// <summary>
		/// Clears tile with particular color, flat normal and no specular.
		/// </summary>
		/// <param name="color"></param>
		public void MakeGlossyMetal()
		{
			colorData.Fill( Color.Gray );
			specularData.PerpixelProcessing( spec => new Color( (byte)0, (byte)255, (byte)0, spec.A ) );

			colorDataMip?.Fill( Color.Gray );
			specularDataMip?.PerpixelProcessing( spec => new Color( (byte)0, (byte)255, (byte)0, spec.A ) );
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

			c	=	colorData.SampleQ4Clamp( x, y );
			n	=	normalData.SampleQ4Clamp( x, y );
			s	=	specularData.SampleQ4Clamp( x, y );
		}



		/// <summary>
		/// Gets GPU-ready data
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public Color[] GetGpuData(int index, int mip) 
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

					colorDataMip	.Write( x, y, c );
					normalDataMip	.Write( x, y, n );
					specularDataMip	.Write( x, y, s );
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
			colorData.Write( x, y, a );
			normalData.Write( x, y, b );
			specularData.Write( x, y, c );
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
			Image.SaveTga( colorData, stream );
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
				colorData		=	new Image(size, size);
				normalData		=	new Image(size, size);
				specularData	=	new Image(size, size);

				var length		=	size * size;

				reader.Read( colorData.RawImageData, length );
				reader.Read( normalData.RawImageData, length );
				reader.Read( specularData.RawImageData, length );

				reader.Read( colorDataMip.RawImageData, length/4 );
				reader.Read( normalDataMip.RawImageData, length/4 );
				reader.Read( specularDataMip.RawImageData, length/4 );
			}
		}





		/// <summary>
		/// Draws text
		/// </summary>
		/// <param name="font"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="text"></param>
		public void DrawText ( Image font, int x, int y, string text )
		{
			for (int i=0; i<text.Length; i++) {

				var ch		=	((int)text[i]) & 0xFF;

				int srcX	=	(ch % 16) * 8;
				int srcY	=	(ch / 16) * 8;
				int dstX	=	x + i * 8;
				int dstY	=	y;

				font.CopySubImageTo( srcX, srcY, 9,8, dstX, dstY, colorData );
			}
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

					normalData.Write( u,v, Color.FlatNormals );
					specularData.Write( u,v, Color.Black );

					if (c==0) {
						colorData.Write( u,v, Color.Black );
					} else {
						colorData.Write( u,v, Color.White );
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

			colorData	.Tint( mipColors[ (mip+0) % count ] );
			colorDataMip.Tint( mipColors[ (mip+1) % count ] );

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
				colorData.Write( b,     i,		Color.Red );
				colorData.Write( b+s-1,	i,		Color.Red );
				colorData.Write( i,		b,      Color.Red );
				colorData.Write( i,		b+s-1,	Color.Red );
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
				colorDataMip.Write( b,     i,		Color.Red );
				colorDataMip.Write( b+s-1,	i,		Color.Red );
				colorDataMip.Write( i,		b,      Color.Red );
				colorDataMip.Write( i,		b+s-1,	Color.Red );
			}
		}

	}
}
