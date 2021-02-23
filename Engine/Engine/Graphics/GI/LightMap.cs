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
	public class LightMap : DisposableBase, ILightmapProvider 
	{
		public struct HeaderData
		{
			public Size2	MapSize;
			public Size3	VolumeSize;
			public int		VolumeStride;
			public Vector3	VolumePosition;
			public int		Reserved0;
		}

		readonly RenderSystem rs;

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

		internal Texture3DCompute	lightVolumeL0;
		internal Texture3DCompute	lightVolumeL1;
		internal Texture3DCompute	lightVolumeL2;
		internal Texture3DCompute	lightVolumeL3;



		readonly Dictionary<string,Rectangle> regions = new Dictionary<string, Rectangle>();

		readonly HeaderData header;
		public HeaderData Header { get { return header; } }


		public IEnumerable<Rectangle> Regions { get { return regions.Select( r => r.Value ); } }


		public LightMap ( RenderSystem rs, Size2 mapSize, Size3 volumeSize )
		{
			this.rs	=	rs;

			header	=	new HeaderData();
			header.MapSize		=	mapSize;
			header.VolumeSize	=	volumeSize;

			CreateGpuResources( mapSize, volumeSize );
		}


		public LightMap ( RenderSystem rs, Stream stream )
		{
			this.rs		=	rs;

			using ( var reader = new BinaryReader( stream ) )
			{
				//	read header :
				reader.ExpectFourCC("RAD4", "bad lightmap format");

				header			=	reader.Read<HeaderData>();

				//	read regions :
				reader.ExpectFourCC("RGN1", "bad lightmap format");

				int regionCount = reader.ReadInt32();
				regions		=	new Dictionary<string, Rectangle>();

				for (int i=0; i<regionCount; i++)
				{
					regions.Add( reader.ReadString(), reader.Read<Rectangle>() );
				}

				//	read regions :
				reader.ExpectFourCC("MAP1", "bad lightmap format");

				var dataSize2	=	header.MapSize.Width * header.MapSize.Height * 4;
				var dataBuffer2	=	new byte[ dataSize2 ];

				reader.Read( dataBuffer2, 0, dataSize2 ); irradianceL0.SetData( dataBuffer2 );
			}
		}


		public void Save ( Stream stream )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				writer.WriteFourCC("RAD4");
				writer.Write( header );

				writer.WriteFourCC("RGN1");
				foreach ( var pair in regions )
				{
					writer.Write( pair.Key );
					writer.Write( pair.Value );
				}

				writer.WriteFourCC("MAP1");

				var dataSize2	=	header.MapSize.Width * header.MapSize.Height * 4;
				var dataBuffer2	=	new byte[ dataSize2 ];

				irradianceL0.GetData( dataBuffer2 );	writer.Write( dataBuffer2, 0, dataSize2 );
				irradianceL1.GetData( dataBuffer2 );	writer.Write( dataBuffer2, 0, dataSize2 );
				irradianceL2.GetData( dataBuffer2 );	writer.Write( dataBuffer2, 0, dataSize2 );
				irradianceL3.GetData( dataBuffer2 );	writer.Write( dataBuffer2, 0, dataSize2 );
			}
		}

		
		void CreateGpuResources( Size2 mapSize, Size3 volumeSize )
		{
			radiance		=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10,	mapSize.Width, mapSize.Height, true,  true );
			irradianceL0	=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10,	mapSize.Width, mapSize.Height, false, true );
			irradianceL1	=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,	mapSize.Width, mapSize.Height, false, true );
			irradianceL2	=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,	mapSize.Width, mapSize.Height, false, true );
			irradianceL3	=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,	mapSize.Width, mapSize.Height, false, true );

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




		public Size2 GetLightmapSize()
		{
			return new Size2( Width, Height );
		}

		public BoundingBox GetVolumeBounds()
		{
			throw new NotImplementedException();
		}

		public Int3 GetVolumeSize()
		{
			return new Int3( Header.VolumeSize.Width, Header.VolumeSize.Height, Header.VolumeSize.Depth );
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
			if (regions.TryGetValue( regionName, out rect ) ) {
				return rect;
			} else {
				Log.Warning("Irradiance map region [{0}] not found", regionName );
				return new Rectangle(0,0,0,0);
			}
		}



		public Vector4 GetRegionMadST ( string regionName )
		{
			return GetRegion(regionName).GetMadOpScaleOffsetNDC( Width, Height );
		}


		static Random rand = new Random();
		Color[] colors = Enumerable.Range(0,64).Select( i => rand.NextColor() ).ToArray();


		public void DebugDraw( int x, int y, DebugRender dr )
		{
			/*int counter = 0;
			for (int i=0; i<tilesX; i++)
			{
				for (int j=0; j<tilesY; j++)
				{
					var min = bboxMinCpu[i,j];
					var max = bboxMaxCpu[i,j];
					dr.DrawBox( new BoundingBox(min, max), Matrix.Identity, colors[ counter % colors.Length ], 2 );
					counter++;
				}
			} */
			//x	=	MathUtil.Clamp( x, 0, width-1 );
			//y	=	MathUtil.Clamp( y, 0, height-1 );

			//var recvPatch	=	new Int2(x,y);

			//var indexCount	=	indexmap_cpu[ recvPatch ];
			//var offset		=	indexCount >> 8;
			//var count		=	indexCount & 0xFF;
			//var	begin		=	offset;
			//var	end			=	offset + count;

			//var origin		=	position_cpu[ recvPatch ];
				
			//for (var i = begin; i<end; i++)
			//{
			//	var radPatch	=	new PatchIndex( indices_cpu[ i ] );
			//	var patchCoord	=	new Int3( radPatch.X, radPatch.Y, radPatch.Mip );

			//	var pos			=	position_cpu[ patchCoord ];

			//	dr.DrawLine( origin, pos, colors[ radPatch.Mip ] );
			//}
		}
	}
}
