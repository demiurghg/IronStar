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

namespace Fusion.Engine.Graphics.Lights 
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

		public readonly MipChain<Color>     Albedo;
		public readonly MipChain<Vector3>   Position;
		public readonly MipChain<Vector3>   Normal;
		public readonly MipChain<float>     Area;
		public readonly Image<byte>         Coverage;
		public readonly Image<Vector3>		Sky;
		public readonly Image<uint>			IndexMap;
		public readonly List<GlobalPatchIndex>	Indices;
		public readonly Image<Int2>			Tiles;
		public readonly Image<Vector3>		BBoxMin;
		public readonly Image<Vector3>		BBoxMax;

		public readonly Volume<Int2>		Clusters;
		public readonly Volume<uint>		IndexVolume;
		public readonly Volume<Vector3>		SkyVolume;

		public readonly List<GlobalPatchIndex>		TileCache;
		public readonly List<CachedPatchIndex>	CachedIndices;

		readonly Allocator2D                allocator;

		public IDictionary<Guid, Rectangle> Regions { get { return regions; } }
		readonly Dictionary<Guid, Rectangle> regions = new Dictionary<Guid, Rectangle>();
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


			Albedo          =   new MipChain<Color>		( size,  size,  RadiositySettings.MapPatchLevels, Color.Zero );
			Position        =   new MipChain<Vector3>	( size,  size,  RadiositySettings.MapPatchLevels, Vector3.Zero );
			Normal          =   new MipChain<Vector3>	( size,  size,  RadiositySettings.MapPatchLevels, Vector3.Zero );
			Area            =   new MipChain<float>		( size,  size,  RadiositySettings.MapPatchLevels, 0 );
			Tiles			=	new Image<Int2>			( TileX, TileY, Int2.Zero );
			BBoxMin			=	new Image<Vector3>		( TileX, TileY, Vector3.Zero );
			BBoxMax			=	new Image<Vector3>		( TileX, TileY, Vector3.Zero );

			Sky             =   new Image<Vector3>		( size, size, Vector3.Zero );
			IndexMap        =   new Image<uint>			( size, size, 0 );
			Indices         =   new List<GlobalPatchIndex>();

			TileCache		=	new List<GlobalPatchIndex>();
			CachedIndices	=	new List<CachedPatchIndex>();;

			Coverage        =   new Image<byte>( size, size, 0 );

			IndexVolume		=	new Volume<uint>	( VolumeWidth,				 VolumeHeight,				 VolumeDepth );
			Clusters		=	new Volume<Int2>	( VolumeWidth / ClusterSize, VolumeHeight / ClusterSize, VolumeDepth / ClusterSize );
			SkyVolume		=	new Volume<Vector3>	( VolumeWidth,				 VolumeHeight,				 VolumeDepth );
		}



		public Int2 AddGlobalPatchIndices ( IEnumerable<GlobalPatchIndex> indices )
		{
			int offset	=	TileCache.Count;
			int count	=	indices.Count();

			TileCache.AddRange( indices );

			return new Int2( offset, count );
		}


		public uint AddCachedPatchIndices ( IEnumerable<CachedPatchIndex> indices )
		{
			int offset	=	CachedIndices.Count;
			int count	=	indices.Count();

			CachedIndices.AddRange( indices );

			return Radiosity.GetLMIndex( offset, count );
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



		public void ComputeBoundingBoxes()
		{

			for (int tx=0; tx<TileX; tx++)
			{
				for (int ty=0; ty<TileY; ty++)
				{
					var coords = new List<Vector3>( TileX * TileY );
					var maxArea = 0f;

					for (int x=0; x<TileSize; x++)
					{
						for (int y=0; y<TileSize; y++)
						{
							var xy = new Int2( tx*TileSize+x, ty*TileSize+y );
							if (Albedo[xy].A>0)
							{
								maxArea = Math.Max( Area[xy], maxArea );
								coords.Add( Position[ xy ] );
							}
						}
					}

					var margin	=	Vector3.One * (float)Math.Sqrt( maxArea );
					var bbox	= BoundingBox.FromPoints( coords );
						bbox	= new BoundingBox( bbox.Minimum - margin, bbox.Maximum + margin );

					BBoxMin[ tx, ty ] = bbox.Minimum;
					BBoxMax[ tx, ty ] = bbox.Maximum;
				}
			}
		}



		public bool SelectPatch( Int2 coord, float dist, float nDotV, RadiositySettings settings, out Int3 selectedPatch )
		{
			selectedPatch		=	new Int3( coord.X, coord.Y, 0 );
			Int3	patch		=	new Int3( coord.X, coord.Y, 0 );

			if (Albedo[selectedPatch].A==0)
			{
				return false;
			}

			for (int mip=1; mip<RadiositySettings.MapPatchLevels; mip++)
			{
				patch.X =	patch.X / 2;
				patch.Y =	patch.Y / 2;
				patch.Z	=	mip;

				var areaThreshold	=	MathUtil.TwoPi * dist*dist * settings.PatchThreshold;

				if ( Albedo[patch].A==0 ) break;
				if ( Area[patch] * nDotV > areaThreshold ) break;

				selectedPatch	=	patch;
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



		public void GeneratePatchLods ()
		{
			for (int srcMip = 0; srcMip < RadiositySettings.MapPatchLevels-1; srcMip++)
			{
				int dstMip		= srcMip+1;
				int dstWidth	= Albedo[dstMip].Width;
				int dstHeight	= Albedo[dstMip].Height;

				for (int i=0; i<dstWidth; i++)
				{
					for (int j=0; j<dstHeight; j++)
					{
						var c00	=	Albedo[srcMip][i*2+0, j*2+0];
						var c01	=	Albedo[srcMip][i*2+0, j*2+1];
						var c10	=	Albedo[srcMip][i*2+1, j*2+0];
						var c11	=	Albedo[srcMip][i*2+1, j*2+1];

						var p00	=	Position[srcMip][i*2+0, j*2+0];
						var p01	=	Position[srcMip][i*2+0, j*2+1];
						var p10	=	Position[srcMip][i*2+1, j*2+0];
						var p11	=	Position[srcMip][i*2+1, j*2+1];

						var n00	=	Normal[srcMip][i*2+0, j*2+0];
						var n01	=	Normal[srcMip][i*2+0, j*2+1];
						var n10	=	Normal[srcMip][i*2+1, j*2+0];
						var n11	=	Normal[srcMip][i*2+1, j*2+1];

						var a00	=	Area[srcMip][i*2+0, j*2+0];
						var a01	=	Area[srcMip][i*2+0, j*2+1];
						var a10	=	Area[srcMip][i*2+1, j*2+0];
						var a11	=	Area[srcMip][i*2+1, j*2+1];

						var w00	=	c00.A == 0 ? 0 : 1;
						var w01	=	c01.A == 0 ? 0 : 1;
						var w10	=	c10.A == 0 ? 0 : 1;
						var w11	=	c11.A == 0 ? 0 : 1;

						var all	=	c00.A!=0 && c01.A!=0 && c10.A!=0 && c11.A!=0;

						var w	=	w00 + w01 + w10 + w11;

						if (all)
						{
							Albedo[dstMip][i,j]		=	AverageColor( c00, c11, c01, c10 );
							Normal[dstMip][i,j]		=	AverageVector( n00, n01, n10, n11 ).Normalized();
							Position[dstMip][i,j]	=	AverageVector( p00, p01, p10, p11 );
							Area[dstMip][i,j]		=	a00 + a01 + a10 + a11;
						}
						else
						{
							Albedo[dstMip][i,j]		=	Color.Zero;
							Normal[dstMip][i,j]		=	Vector3.Zero;
							Position[dstMip][i,j]	=	Vector3.Zero;
							Area[dstMip][i,j]		=	0;
						}
					}
				}
			}
		}
		


		void SaveDebugImage( Image<Color> image, string name )
		{
			ImageLib.SaveTga( image, name + ".tga" );
		}



		Color EncodeNormalRGB8( Vector3 n )
		{
			n.Normalize();
			n = (n + Vector3.One) * 0.5f;
			return new Color( 
				MathUtil.Lerp( (byte)0, (byte)255, n.X ),
				MathUtil.Lerp( (byte)0, (byte)255, n.Y ),
				MathUtil.Lerp( (byte)0, (byte)255, n.Z ),
				(byte)255 );
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


		Int2 DebugDrawTileInfo( int x, int y, Int2 tileData )
		{
			var ts = RadiositySettings.TileSize;

			if (tileData.Y<1) return tileData;

			var testImage = new Image<Color>(tileData.Y, ts*ts, Color.Black);

			int row = 0;

			for (uint i=0; i<ts*ts; i++)
			{
				var xy = MortonCode.Decode2(i) + new Int2( x * ts, y * ts );

				var addr = IndexMap[xy];
				var offset = addr >> 8;
				var count = addr & 0xFF;

				if (count==0) continue;

				for (uint j=offset; j<offset+count; j++)
				{
					var cpatch	= CachedIndices[(int)j];
					var hits	= (byte)cpatch.HitCount;
					var normal	= (byte)cpatch.Direction;

					testImage[cpatch.CacheIndex, row] = new Color( hits*8, 0, normal*4, (byte)255 );
				}

				row++;
			}

			testImage = testImage.Crop( new Rectangle( 0,0,testImage.Width, row), Color.Black);

			SaveDebugImage( testImage, string.Format("tile_{0:00}_{1:00}_{2:0000}", x, y, tileData.Y) );

			return tileData;
		}


		public void SaveDebugImages()
		{
			Tiles.ForEachPixel( DebugDrawTileInfo );
			
			//File.WriteAllText( "rad_indices.txt", string.Join("\r\n", Indices.Select( idx=>idx.ToString() ) ) );

			SaveDebugImage( Sky.Convert( EncodeSkyRGB8 ), "rad_sky" );
			SaveDebugImage( IndexMap.Convert( idx => new Color(idx) ), "rad_index_map" );

			for (int mip=0; mip<RadiositySettings.MapPatchLevels; mip++)
			{
				var prefix = "_" + mip.ToString();
				SaveDebugImage( Albedo[mip]										, "rad_albedo" + prefix );
				SaveDebugImage( Normal[mip].Convert( EncodeNormalRGB8 )			, "rad_normal" + prefix );
				SaveDebugImage( Area[mip].Convert( a => new Color(a/256.0f))	, "rad_area"   + prefix );
			}

			var rand = new Random();
			SkyVolume.ForEachVoxel( (i,j,k,c) => rand.NextVector3OnSphere() );

			int z = 0;
			foreach (var slice in SkyVolume.Convert(EncodeSkyRGB8).GetSlices())
			{
				SaveDebugImage( slice, "sky" + z.ToString() );
				z++;
			}
		}


		/*-----------------------------------------------------------------------------------------
		 *	Lightmap Import :
		-----------------------------------------------------------------------------------------*/

		public void WriteStream ( Stream stream )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				const int mips = RadiositySettings.MapPatchLevels;

				//	write header :
				writer.WriteFourCC("RAD2");

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
				for (int i=0; i<mips; i++) Position[i].WriteStream( stream );

				writer.WriteFourCC("NRM1");
				for (int i=0; i<mips; i++) Normal[i].Convert( EncodeNormalRGB8 ).WriteStream( stream );

				writer.WriteFourCC("ALB1");
				for (int i=0; i<mips; i++) Albedo[i].WriteStream( stream );

				writer.WriteFourCC("ARE1");
				for (int i=0; i<mips; i++) Area[i].WriteStream( stream );

				writer.WriteFourCC("SKY1");
				Sky.Convert( EncodeSkyRGB8 ).WriteStream( stream );

				//	write tiled cache data :
				writer.WriteFourCC("TILE");
				writer.Write( TileCache.Count );
				writer.Write( TileCache.Select( a => a.Index ).ToArray() );
				writer.Write( CachedIndices.Count );
				writer.Write( CachedIndices.Select( a => a.GpuIndex ).ToArray() );

				//	write tile & index map
				writer.WriteFourCC("MAP1");
				Tiles.WriteStream( stream );
				IndexMap.WriteStream( stream );

				//	bounding boxes :
				writer.WriteFourCC("BBOX");
				BBoxMin.WriteStream( stream );
				BBoxMax.WriteStream( stream );

				//	write cluster & index volumes
				writer.WriteFourCC("VOL1");
				Clusters.WriteStream( stream );
				IndexVolume.WriteStream( stream );
				SkyVolume.Convert( EncodeSkyRGB8 ).WriteStream( stream );
			}
		}

	}

}
