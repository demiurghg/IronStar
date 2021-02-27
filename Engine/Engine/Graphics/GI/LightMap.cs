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
using Fusion.Core.Configuration;
using Fusion.Build.Mapping;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Content;
using Fusion.Engine.Graphics.GI;
using Fusion.Engine.Graphics.GI2;

namespace Fusion.Engine.Graphics.GI {

	[ContentLoader(typeof(LightMap))]
	public class LightMapLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return new LightMap( content.Game.RenderSystem, stream );
		}
	}

	// #TODO -- rename to FormFactor
	public class LightMap : DisposableBase, ILightMapProvider 
	{
		public struct HeaderData
		{
			public Size2	MapSize;
			public Size3	VolumeSize;
			public Matrix	VolumeMatrix;
		}

		readonly RenderSystem rs;

		public Size2 LightMapSize { get { return header.MapSize; } }
		public Size3 VolumeSize { get { return header.VolumeSize; } }

		public int Width  { get { return header.MapSize.Width; } }
		public int Height { get { return header.MapSize.Height; } }

		int	VolumeWidth  { get { return header.VolumeSize.Width ; } }
		int	VolumeHeight { get { return header.VolumeSize.Height; } }
		int	VolumeDepth  { get { return header.VolumeSize.Depth ; } }

		internal RenderTarget2D		radiance	;
		internal RenderTarget2D		irradianceL0;
		internal RenderTarget2D		irradianceL1;
		internal RenderTarget2D		irradianceL2;
		internal RenderTarget2D		irradianceL3;

		internal RenderTarget2D		tempHdr;
		internal RenderTarget2D		tempLdr;

		internal Texture3DCompute	lightVolumeL0;
		internal Texture3DCompute	lightVolumeL1;
		internal Texture3DCompute	lightVolumeL2;
		internal Texture3DCompute	lightVolumeL3;



		readonly Dictionary<string,Rectangle> regions = new Dictionary<string, Rectangle>();

		readonly HeaderData header;
		public HeaderData Header { get { return header; } }

		public Dictionary<string,Rectangle> Regions { get { return regions; } }

		public LightMap ( RenderSystem rs, Size2 mapSize, Size3 volumeSize, Matrix volumeMatrix )
		{
			this.rs	=	rs;

			header	=	new HeaderData();
			header.MapSize		=	mapSize;
			header.VolumeSize	=	volumeSize;
			header.VolumeMatrix	=	volumeMatrix;

			CreateGpuResources( mapSize, volumeSize, true );
		}


		public LightMap ( RenderSystem rs, Stream stream )
		{
			this.rs		=	rs;

			using ( var reader = new BinaryReader( stream ) )
			{
				//	read header :
				reader.ExpectFourCC("RAD5", "bad lightmap format");

				header			=	reader.Read<HeaderData>();

				//	read regions :
				reader.ExpectFourCC("RGN1", "bad lightmap format");

				int regionCount = reader.ReadInt32();
				regions		=	new Dictionary<string, Rectangle>();

				for (int i=0; i<regionCount; i++)
				{
					regions.Add( reader.ReadString(), reader.Read<Rectangle>() );
				}

				CreateGpuResources( header.MapSize, header.VolumeSize, false );

				//	read map :
				reader.ExpectFourCC("MAP1", "bad lightmap format");

				var dataSize2	=	header.MapSize.TotalArea * 4;
				var dataBuffer2	=	new byte[ dataSize2 ];

				reader.Read( dataBuffer2, 0, dataSize2 ); irradianceL0.SetData( dataBuffer2 );
				reader.Read( dataBuffer2, 0, dataSize2 ); irradianceL1.SetData( dataBuffer2 );
				reader.Read( dataBuffer2, 0, dataSize2 ); irradianceL2.SetData( dataBuffer2 );
				reader.Read( dataBuffer2, 0, dataSize2 ); irradianceL3.SetData( dataBuffer2 );

				//	read volume :
				reader.ExpectFourCC("VOL1", "bad lightmap format");
				var dataSize3	=	header.VolumeSize.TotalVolume * 4;
				var dataBuffer3	=	new byte[ dataSize2 ];

				reader.Read( dataBuffer3, 0, dataSize3 ); lightVolumeL0.SetData( dataBuffer3 );
				reader.Read( dataBuffer3, 0, dataSize3 ); lightVolumeL1.SetData( dataBuffer3 );
				reader.Read( dataBuffer3, 0, dataSize3 ); lightVolumeL2.SetData( dataBuffer3 );
				reader.Read( dataBuffer3, 0, dataSize3 ); lightVolumeL3.SetData( dataBuffer3 );
			}
		}


		public void Save ( Stream stream )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				writer.WriteFourCC("RAD5");
				writer.Write( header );

				writer.WriteFourCC("RGN1");
				writer.Write( regions.Count );

				foreach ( var pair in regions )
				{
					writer.Write( pair.Key );
					writer.Write( pair.Value );
				}

				writer.WriteFourCC("MAP1");

				var dataSize2	=	header.MapSize.TotalArea * 4;
				var dataBuffer2	=	new byte[ dataSize2 ];

				irradianceL0.GetData( dataBuffer2 );	writer.Write( dataBuffer2, 0, dataSize2 );
				irradianceL1.GetData( dataBuffer2 );	writer.Write( dataBuffer2, 0, dataSize2 );
				irradianceL2.GetData( dataBuffer2 );	writer.Write( dataBuffer2, 0, dataSize2 );
				irradianceL3.GetData( dataBuffer2 );	writer.Write( dataBuffer2, 0, dataSize2 );

				writer.WriteFourCC("VOL1");

				var dataSize3	=	header.VolumeSize.TotalVolume * 4;
				var dataBuffer3	=	new byte[ dataSize2 ];

				lightVolumeL0.GetData( dataBuffer3 );	writer.Write( dataBuffer3, 0, dataSize3 );
				lightVolumeL1.GetData( dataBuffer3 );	writer.Write( dataBuffer3, 0, dataSize3 );
				lightVolumeL2.GetData( dataBuffer3 );	writer.Write( dataBuffer3, 0, dataSize3 );
				lightVolumeL3.GetData( dataBuffer3 );	writer.Write( dataBuffer3, 0, dataSize3 );
			}
		}

		
		void CreateGpuResources( Size2 mapSize, Size3 volumeSize, bool createTempResources )
		{
			radiance		=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10,	mapSize.Width, mapSize.Height, false, true );
			irradianceL0	=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10,	mapSize.Width, mapSize.Height, false, true );
			irradianceL1	=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,	mapSize.Width, mapSize.Height, false, true );
			irradianceL2	=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,	mapSize.Width, mapSize.Height, false, true );
			irradianceL3	=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,	mapSize.Width, mapSize.Height, false, true );

			if (createTempResources)
			{
				tempHdr		=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10,	mapSize.Width, mapSize.Height, false, true );
				tempLdr		=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,	mapSize.Width, mapSize.Height, false, true );
			}

			rs.Device.Clear( radiance.Surface,		Color4.Zero );
			rs.Device.Clear( irradianceL0.Surface,	Color4.Zero );
			rs.Device.Clear( irradianceL1.Surface,	Color4.Zero );
			rs.Device.Clear( irradianceL2.Surface,	Color4.Zero );
			rs.Device.Clear( irradianceL3.Surface,	Color4.Zero );

			lightVolumeL0	=	new Texture3DCompute( rs.Device, ColorFormat.Rg11B10,	volumeSize.Width, volumeSize.Height, volumeSize.Depth );
			lightVolumeL1	=	new Texture3DCompute( rs.Device, ColorFormat.Rgba8,		volumeSize.Width, volumeSize.Height, volumeSize.Depth );
			lightVolumeL2	=	new Texture3DCompute( rs.Device, ColorFormat.Rgba8,		volumeSize.Width, volumeSize.Height, volumeSize.Depth );
			lightVolumeL3	=	new Texture3DCompute( rs.Device, ColorFormat.Rgba8,		volumeSize.Width, volumeSize.Height, volumeSize.Depth );
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref radiance		);
				SafeDispose( ref irradianceL0	);
				SafeDispose( ref irradianceL1	);
				SafeDispose( ref irradianceL2	);
				SafeDispose( ref irradianceL3	);
				
				SafeDispose( ref lightVolumeL0	);
				SafeDispose( ref lightVolumeL1	);
				SafeDispose( ref lightVolumeL2	);
				SafeDispose( ref lightVolumeL3	);
			}

			base.Dispose( disposing );
		}


		public ShaderResource GetLightmap( int band )
		{
			switch (band)
			{
				case 0: return irradianceL0;
				case 1: return irradianceL1;
				case 2: return irradianceL2;
				case 3: return irradianceL3;
			}
			throw new ArgumentOutOfRangeException("band");
		}

		
		public ShaderResource GetVolume( int band )
		{
			switch (band)
			{
				case 0: return lightVolumeL0;
				case 1: return lightVolumeL1;
				case 2: return lightVolumeL2;
				case 3: return lightVolumeL3;
			}
			throw new ArgumentOutOfRangeException("band");
		}


		public void AddRegion( string name, Rectangle region )
		{
			regions.Add( name, region );
		}


		public bool HasRegion ( string name )
		{
			return regions.ContainsKey(name);
		}


		public Rectangle GetRegion ( string regionName )
		{
			Rectangle rect;
			if (regions.TryGetValue( regionName, out rect ) ) 
			{
				return rect;
			}
			else 
			{
				Log.Warning("Irradiance map region [{0}] not found", regionName );
				return new Rectangle(0,0,0,0);
			}
		}


		public Vector4 GetRegionMadST ( string regionName )
		{
			return GetRegion(regionName).GetMadOpScaleOffsetNDC( Width, Height );
		}


		public Matrix WorldToVolume
		{
			get
			{
				var width		=	Header.VolumeSize.Width;
				var height		=	Header.VolumeSize.Height;
				var depth		=	Header.VolumeSize.Depth;

				var scale		=	new Vector3( 1.0f / width, 1.0f / height, 1.0f / depth );
				var half		=	new Vector3( 0.5f, 0.5f, 0.5f );

				var translation	=	Matrix.Translation( half + 0.0f*scale );
				var worldInv	=	Matrix.Invert( Header.VolumeMatrix );

				return worldInv * translation;
			}
		}

		public Matrix VoxelToWorld
		{
			get
			{
				var width		=	Header.VolumeSize.Width;
				var height		=	Header.VolumeSize.Height;
				var depth		=	Header.VolumeSize.Depth;

				var scale		=	new Vector3( 1.0f / width, 1.0f / height, 1.0f / depth );
				var half		=	new Vector3( -0.5f, -0.5f, -0.5f );

				var scaling		=	Matrix.Scaling( scale );
				var translation	=	Matrix.Translation( half + scale * 0.5f );
				var world		=	Header.VolumeMatrix;
				return scaling * translation * world;
			}
		}
	}
}
