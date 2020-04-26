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

namespace Fusion.Engine.Graphics.Lights 
{
	public class LightMapContent 
	{
		public readonly int Width;
		public readonly int Height;

		public readonly MipChain<Color>		Albedo;
		public readonly MipChain<Vector3>	Position;
		public readonly MipChain<Vector3>	Normal;
		public readonly MipChain<float>		Area;
		public readonly Image<byte>			Coverage;
		public readonly Image<byte>			PatchSize;
		public readonly Image<Vector3>		Sky;
		public readonly Image<uint>			IndexMap;
		public readonly List<uint>			Indices;

		readonly Allocator2D				allocator;

		public IDictionary<Guid,Rectangle>	Regions { get { return regions; } }
		readonly Dictionary<Guid, Rectangle> regions = new Dictionary<Guid, Rectangle>();
		readonly Dictionary<string, uint> indexDict = new Dictionary<string, uint>();
		
		/// <summary>
		/// Creates instance of the lightmap g-buffer :
		/// </summary>
		/// <param name="w"></param>
		/// <param name="h"></param>
		public LightMapContent( int size ) 
		{
			Width			=	size;
			Height			=	size;

			allocator		=	new Allocator2D( size );

			Albedo			=	new MipChain<Color>		( size, size, RadiositySettings.MapPatchLevels, Color.Zero	);
			Position		=	new MipChain<Vector3>	( size, size, RadiositySettings.MapPatchLevels, Vector3.Zero	);
			Normal			=	new MipChain<Vector3>	( size, size, RadiositySettings.MapPatchLevels, Vector3.Zero	);
			Area			=	new MipChain<float>		( size, size, RadiositySettings.MapPatchLevels, 0 );

			Sky				=	new Image<Vector3>		( size, size, Vector3.Zero );
			IndexMap		=	new Image<uint>			( size, size, 0 );
			Indices			=	new List<uint>			();

			Coverage		=	new Image<byte>			( size, size, 0 );
			PatchSize		=	new Image<byte>			( size, size, 0 );
		}
			

		public uint AddFormFactorPatchIndices( uint[] patches )
		{
			patches	=	patches.Distinct().OrderBy( k => k ).ToArray();
			var key	=	string.Join("-", patches.Select( p => p.ToString() ) );

			uint index;
			int offset;
			int count;

			if (indexDict.TryGetValue(key, out index)) 
			{
				return index;
			}
			else
			{
				offset	= Indices.Count;
				count	= patches.Length;
				index	= Radiosity.GetLMIndex( offset, count );
				Indices.AddRange( patches );
				Indices.Add(0xAAAAAAAA);
				indexDict.Add( key, index );
				return index;
			}
		}


		public bool IsRegionCollapsable( Rectangle rect )
		{
			for (int i=rect.Left; i<=rect.Right; i++)
			{
				for (int j=rect.Top; j<rect.Bottom; j++)
				{
					if (Albedo[i,j].A==0)
					{
						return false;
					}
				}
			}
			return true;
		}



		public void ComputePatchSizes()
		{
			for ( byte sz = 1; sz <=32; sz*=2 )
			{
				ComputePatchSize(sz);
			}
		}


		void ComputePatchSize(byte size)
		{
			int w = Albedo.Width;
			int h = Albedo.Height;

			for (int i=0; i<w; i+=size)
			{
				for (int j=0; j<w; j+=size)
				{
					var rect = new Rectangle(i,j,size,size);
					if (IsRegionCollapsable(rect))
					{
						PatchSize.FillRect( rect, size );
					}
				}
			}
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
							Position[dstMip][i,j]	=	AverageVector( n00, n01, n10, n11 );
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


		public void SaveDebugImages()
		{
			File.WriteAllText( "rad_indices.txt", string.Join("\r\n", Indices.Select( idx=>idx.ToString("X8") ) ) );

			SaveDebugImage( PatchSize.Convert( size => new Color(size,size,size,(byte)255) ), "rad_patchSize" );

			SaveDebugImage( Sky.Convert( EncodeSkyRGB8 ), "rad_sky" );
			SaveDebugImage( IndexMap.Convert( idx => new Color(idx) ), "rad_index_map" );

			for (int mip=0; mip<RadiositySettings.MapPatchLevels; mip++)
			{
				var prefix = "_" + mip.ToString();
				SaveDebugImage( Albedo[mip]										, "rad_albedo" + prefix );
				SaveDebugImage( Normal[mip].Convert( EncodeNormalRGB8 )			, "rad_normal" + prefix );
				SaveDebugImage( Area[mip].Convert( a => new Color(a/256.0f))	, "rad_area"   + prefix );
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
				writer.WriteFourCC("RAD1");

				writer.Write( Width );
				writer.Write( Height );

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

				//	write index map
				writer.WriteFourCC("MAP1");
				IndexMap.WriteStream( stream );

				// #TODO #LIGHTMAPS - write volume indices
				writer.WriteFourCC("VOL1");

				//	write indices
				writer.WriteFourCC("IDX1");
				writer.Write( Indices.Count );
				writer.Write( Indices.ToArray() );
			}
		}

	}

}
