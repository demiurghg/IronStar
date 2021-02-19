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
	public class FormFactorLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return new LightMap( content.Game.RenderSystem, stream );
		}
	}

	// #TODO -- rename to FormFactor
	public class LightMap : DisposableBase, ILightmapProvider {

		readonly RenderSystem rs;

		public int Width  { get { return header.Width; } }
		public int Height { get { return header.Height; } }
		int	tilesX { get { return Width  / RadiositySettings.TileSize; } }
		int	tilesY { get { return Height / RadiositySettings.TileSize; } }

		int	VolumeWidth  { get { return header.VolumeWidth ; } }
		int	VolumeHeight { get { return header.VolumeHeight; } }
		int	VolumeDepth  { get { return header.VolumeDepth ; } }

		int	clusterX { get { return header.VolumeWidth  / RadiositySettings.ClusterSize; } }
		int	clusterY { get { return header.VolumeHeight / RadiositySettings.ClusterSize; } }
		int	clusterZ { get { return header.VolumeDepth  / RadiositySettings.ClusterSize; } }

		//	#TODO -- make private and gain access through properties:
		internal Texture2D			albedo		;
		internal Texture2D			position	;
		internal Texture2D			normal		;

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

		readonly FormFactor.Header header;
		public FormFactor.Header Header { get { return header; } }


		public IEnumerable<Rectangle> Regions { get { return regions.Select( r => r.Value ); } }


		public LightMap ( RenderSystem rs, Stream stream )
		{
			this.rs		=	rs;

			using ( var reader = new BinaryReader( stream ) )
			{
				//	read header :
				reader.ExpectFourCC("RAD3", "bad lightmap format");

				header			=	reader.Read<FormFactor.Header>();

				albedo			=	new Texture2D( rs.Device, Width,  Height, ColorFormat.Rgba8,	1,	false );
				position		=	new Texture2D( rs.Device, Width,  Height, ColorFormat.Rgb32F,	1,	false );
				normal			=	new Texture2D( rs.Device, Width,  Height, ColorFormat.Rgba8,	1,	false );

				radiance		=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10,	Width, Height, true,  true );
				irradianceL0	=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10,	Width, Height, false, true );
				irradianceL1	=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,	Width, Height, false, true );
				irradianceL2	=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,	Width, Height, false, true );
				irradianceL3	=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,	Width, Height, false, true );

				rs.Device.Clear( radiance.Surface,		Color4.Zero );
				rs.Device.Clear( irradianceL0.Surface,	Color4.Zero );
				rs.Device.Clear( irradianceL1.Surface,	Color4.Zero );
				rs.Device.Clear( irradianceL2.Surface,	Color4.Zero );
				rs.Device.Clear( irradianceL3.Surface,	Color4.Zero );

				lightVolumeL0	=	new Texture3DCompute( rs.Device, ColorFormat.Rg11B10,	VolumeWidth, VolumeHeight, VolumeDepth );
				lightVolumeL1	=	new Texture3DCompute( rs.Device, ColorFormat.Rgba8,		VolumeWidth, VolumeHeight, VolumeDepth );
				lightVolumeL2	=	new Texture3DCompute( rs.Device, ColorFormat.Rgba8,		VolumeWidth, VolumeHeight, VolumeDepth );
				lightVolumeL3	=	new Texture3DCompute( rs.Device, ColorFormat.Rgba8,		VolumeWidth, VolumeHeight, VolumeDepth );

				//	read regions :
				reader.ExpectFourCC("RGN1", "bad lightmap format");

				int regionCount = reader.ReadInt32();
				regions		=	new Dictionary<string, Rectangle>();

				for (int i=0; i<regionCount; i++)
				{
					regions.Add( reader.ReadString(), reader.Read<Rectangle>() );
				}

				//	read gbuffer :
				reader.ExpectFourCC("GBF1", "bad lightmap format");

				reader.ExpectFourCC("POS1", "bad lightmap format");
				position.SetData( Image<Vector3>.FromStream(stream).RawImageData );

				reader.ExpectFourCC("NRM1", "bad lightmap format");
				normal.SetData( Image<Color>.FromStream(stream).RawImageData );

				reader.ExpectFourCC("ALB1", "bad lightmap format");
				albedo.SetData( Image<Color>.FromStream(stream).RawImageData );
			}
		}


		FormattedBuffer ReadUintBufferFromStream( BinaryReader reader )
		{
			int count	=	reader.ReadInt32();
			var buffer	=	new FormattedBuffer( rs.Device, Drivers.Graphics.VertexFormat.UInt, count, StructuredBufferFlags.None );
			buffer.SetData( reader.Read<uint>(count) );
			return buffer;
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref albedo		);
				SafeDispose( ref position	);
				SafeDispose( ref normal		);

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
			return new Int3( Header.VolumeWidth, Header.VolumeHeight, Header.VolumeDepth );
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
