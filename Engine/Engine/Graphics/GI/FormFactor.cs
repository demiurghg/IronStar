using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Graphics.Ubershaders;
using Native.Embree;
using System.Runtime.InteropServices;
using Fusion.Core;
using System.Diagnostics;
using Fusion.Engine.Imaging;
using Fusion.Build.Mapping;
using Fusion.Engine.Graphics.GI;
using System.IO;
using Fusion.Core.Content;
using Fusion.Build;

namespace Fusion.Engine.Graphics.GI 
{
	public class FormFactor
	{
		public struct Header
		{
			public int		Width;
			public int		Height;
			public int		LightMapSampleCount;
			public int		Reserved0;

			public int		VolumeWidth;
			public int		VolumeHeight;
			public int		VolumeDepth;
			public int		VolumeStride;
			public Vector3	VolumePosition;
			public float	VolumeSampleCount;
		}

		const int RegionSize	= RadiositySettings.UpdateRegionSize;
		const int TileSize		= RadiositySettings.TileSize;	
		const int ClusterSize	= RadiositySettings.ClusterSize;	

		public int Width		{ get { return header.Width; } }
		public int Height		{ get { return header.Height; } }

		public int VolumeWidth	{ get { return header.VolumeWidth; } }
		public int VolumeHeight	{ get { return header.VolumeHeight; } }
		public int VolumeDepth	{ get { return header.VolumeDepth; } }
		public int VolumeStride	{ get { return header.VolumeStride; } }

		public int TileX { get { return Width  / TileSize; } }
		public int TileY { get { return Height / TileSize; } }

		public readonly Image<Color>		Albedo;
		public readonly Image<Vector3>		Position;
		public readonly Image<Vector3>		Normal;
		public readonly Image<byte>			Coverage;

		public readonly Volume<Int4>		Clusters;
		public readonly Volume<uint>		IndexVolume;
		public readonly Volume<Vector3>		SkyVolume;

		public readonly Image<Color>[]		FormFactorPages;

		readonly Allocator2D				allocator;

		public IDictionary<string, Rectangle> Regions { get { return regions; } }
		readonly Dictionary<string, Rectangle> regions = new Dictionary<string, Rectangle>();
		readonly Dictionary<string, uint> indexDict = new Dictionary<string, uint>();

		public readonly Header header;

		/// <summary>
		/// Creates instance of the lightmap g-buffer :
		/// </summary>
		/// <param name="w"></param>
		/// <param name="h"></param>
		public FormFactor( int size, RadiositySettings settings )
		{
			allocator       =   new Allocator2D( size );

			header	=	new Header();

			header.Width				=	size;
			header.Height				=	size;
			header.LightMapSampleCount	=	settings.LightMapSampleCount;
			header.Reserved0			=	0;

			header.VolumeWidth			=	settings.LightGridWidth;
			header.VolumeHeight			=	settings.LightGridHeight;
			header.VolumeDepth			=	settings.LightGridDepth;
			header.VolumeStride			=	settings.LightGridStep;
			header.VolumePosition		=	Vector3.Zero;
			header.VolumeSampleCount	=	settings.LightGridSampleCount;

			int ffPageCount		=	(size / RegionSize) * (size / RegionSize);
			FormFactorPages		=	Enumerable.Range( 0, ffPageCount )
									.Select( i => new Image<Color>( RegionSize, RegionSize, Color.Black ) )
									.ToArray();

			Albedo			=	   new Image<Color>		( size,  size,  Color.Zero );
			Position		=	   new Image<Vector3>	( size,  size,  Vector3.Zero );
			Normal			=	   new Image<Vector3>	( size,  size,  Vector3.Zero );
			Coverage		=	   new Image<byte>		( size,  size,  0 );

			IndexVolume		=	new Volume<uint>	( VolumeWidth,				 VolumeHeight,				 VolumeDepth );
			Clusters		=	new Volume<Int4>	( VolumeWidth / ClusterSize, VolumeHeight / ClusterSize, VolumeDepth / ClusterSize );
			SkyVolume		=	new Volume<Vector3>	( VolumeWidth,				 VolumeHeight,				 VolumeDepth );
		}



		public bool IsRegionCollapsable( Rectangle rect )
		{
			for ( int i = rect.Left; i<=rect.Right; i++ )
			{
				for ( int j = rect.Top; j<rect.Bottom; j++ )
				{
					if ( Albedo[i, j].A==0 )
					{
						return false;
					}
				}
			}
			return true;
		}



		Color AverageColor( Color c0, Color c1, Color c2, Color c3 )
		{
			return Color.Lerp( 
				Color.Lerp( c0, c1, 0.5f ),
				Color.Lerp( c2, c3, 0.5f ),
				0.5f);
		}



		Vector3 AverageVector( Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3 )
		{
			return Vector3.Lerp( 
				Vector3.Lerp( v0, v1, 0.5f ),
				Vector3.Lerp( v2, v3, 0.5f ),
				0.5f);
		}



		void SaveDebugImage( Image<Color> image, string name )
		{
			ImageLib.SaveTga( image, name + ".tga" );
		}



		Color EncodeNormalRGB8( Vector3 n )
		{
			n.Normalize();
			n = (n + Vector3.One) * 0.5f;
			return new Color( n.X, n.Y, n.Z, 0.0f );
		}



		Color EncodeSkyRGB8( Vector3 n )
		{
			n = (n + Vector3.One) * 0.5f;
			return new Color( 
				MathUtil.Lerp( (byte)0, (byte)255, n.X ),
				MathUtil.Lerp( (byte)0, (byte)255, n.Y ),
				MathUtil.Lerp( (byte)0, (byte)255, n.Z ),
				(byte)255 );
		}


		public void SaveDebugImages()
		{
			SaveDebugImage( Albedo									, "rad_albedo" );
			SaveDebugImage( Normal.Convert( EncodeNormalRGB8 )		, "rad_normal" );
		}


		/*-----------------------------------------------------------------------------------------
		 *	Lightmap Import :
		-----------------------------------------------------------------------------------------*/

		public void WriteStream ( Stream stream )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				//	write header :
				writer.WriteFourCC("RAD3");

				writer.Write( header );

				//	write regions :
				writer.WriteFourCC("RGN1");

				writer.Write( Regions.Count );
				foreach ( var pair in Regions ) {
					writer.Write( pair.Key );
					writer.Write( pair.Value );
				}

				//	write gbuffer :
				writer.WriteFourCC("GBF1");

				writer.WriteFourCC("POS1");
				Position.WriteStream( stream );

				writer.WriteFourCC("NRM1");
				Normal.Convert( EncodeNormalRGB8 ).WriteStream( stream );

				writer.WriteFourCC("ALB1");
				Albedo.WriteStream( stream );
			}
		}

	}

}
