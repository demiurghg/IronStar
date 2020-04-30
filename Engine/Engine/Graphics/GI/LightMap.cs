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

namespace Fusion.Engine.Graphics.Lights {

	// #TODO -- rename to FormFactorLoader
	[ContentLoader(typeof(LightMap))]
	public class IrradianceMapLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return new LightMap( content.Game.RenderSystem, stream );
		}
	}

	// #TODO -- rename to FormFactor
	public class LightMap : DisposableBase {

		readonly RenderSystem rs;

		readonly int	width;
		readonly int	height;

		public int Width { get { return width; } }
		public int Height { get { return height; } }

		//	#TODO -- make private and gain access through properties:
		internal Texture2D			albedo		;
		internal Texture2D			position	;
		internal Texture2D			normal		;
		internal Texture2D			area		;
		internal Texture2D			sky			;
		internal Texture2D			indexMap	;
		internal Texture3D			indexVol	;
		internal FormattedBuffer	indices		;

		internal Texture2D	IrradianceTextureRed	{ get { return albedo; } }
		internal Texture2D	IrradianceTextureGreen	{ get { return normal; } }
		internal Texture2D	IrradianceTextureBlue	{ get { return sky; } }

		readonly Dictionary<Guid,Rectangle> regions = new Dictionary<Guid, Rectangle>();

		MipChain<Vector3>	position_cpu;
		MipChain<Color>		normals_cpu;
		Image<uint>			indexmap_cpu;
		uint[]				indices_cpu	;



		public LightMap ( RenderSystem rs, Stream stream )
		{
			this.rs		=	rs;

			using ( var reader = new BinaryReader( stream ) )
			{
				const int mips = RadiositySettings.MapPatchLevels;

				//	write header :
				reader.ExpectFourCC("RAD1", "bad lightmap format");

				width	=	reader.ReadInt32();
				height	=	reader.ReadInt32();

				albedo		=	new Texture2D( rs.Device, width, height, ColorFormat.Rgba8,		mips,	false );
				position	=	new Texture2D( rs.Device, width, height, ColorFormat.Rgb32F,	mips,	false );
				normal		=	new Texture2D( rs.Device, width, height, ColorFormat.Rgba8,		mips,	false );
				area		=	new Texture2D( rs.Device, width, height, ColorFormat.R32F,		mips,	false );
				sky			=	new Texture2D( rs.Device, width, height, ColorFormat.Rgba8,		1,		false );
				indexMap	=	new Texture2D( rs.Device, width, height, ColorFormat.R32,		1,		false );
				// #TODO #LIGHTMAP -- create volume

				//	read regions :
				reader.ExpectFourCC("RGN1", "bad lightmap format");

				int regionCount = reader.ReadInt32();
				regions		=	new Dictionary<Guid, Rectangle>();

				for (int i=0; i<regionCount; i++)
				{
					regions.Add( reader.Read<Guid>(), reader.Read<Rectangle>() );
				}

				//	write gbuffer :
				reader.ExpectFourCC("GBF1", "bad lightmap format");

				reader.ExpectFourCC("POS1", "bad lightmap format");
				position_cpu = new MipChain<Vector3>( width, height, mips, Vector3.Zero );
				for (int i=0; i<mips; i++) 
				{
					position_cpu[i]	=	Image<Vector3>.FromStream(stream);
					position.SetData( i, position_cpu[i].RawImageData );
				}

				reader.ExpectFourCC("NRM1", "bad lightmap format");
				normals_cpu	=	new MipChain<Color>( width, height, mips, Color.Zero );
				for (int i=0; i<mips; i++) 
				{	
					normals_cpu[i]	=	Image<Color>.FromStream(stream);
					normal.SetData( i, normals_cpu[i].RawImageData );
				}

				reader.ExpectFourCC("ALB1", "bad lightmap format");
				for (int i=0; i<mips; i++) albedo.SetData( i, Image<Color>.FromStream(stream).RawImageData );

				reader.ExpectFourCC("ARE1", "bad lightmap format");
				for (int i=0; i<mips; i++) area.SetData( i, Image<float>.FromStream(stream).RawImageData );

				reader.ExpectFourCC("SKY1", "bad lightmap format");
				sky.SetData( Image<Color>.FromStream(stream).RawImageData );

				//	write index map
				reader.ExpectFourCC("MAP1", "bad lightmap format");
				indexmap_cpu	=	Image<uint>.FromStream(stream);
				indexMap.SetData( indexmap_cpu.RawImageData );

				// #TODO #LIGHTMAPS - write volume indices
				reader.ExpectFourCC("VOL1", "bad lightmap format");

				//	write indices
				reader.ExpectFourCC("IDX1", "bad lightmap format");
				int numIndices = reader.ReadInt32();

				indices	=	new FormattedBuffer( rs.Device, Drivers.Graphics.VertexFormat.UInt, numIndices, StructuredBufferFlags.None );
				indices_cpu	=	reader.Read<uint>(numIndices);
				indices.SetData( indices_cpu );
			}
		}



		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref albedo		);
				SafeDispose( ref position	);
				SafeDispose( ref normal		);
				SafeDispose( ref area		);
				SafeDispose( ref sky		);	
				SafeDispose( ref indexMap	);
				SafeDispose( ref indexVol	);
				SafeDispose( ref indices	);
			}

			base.Dispose( disposing );
		}



		public void AddRegion( Guid guid, Rectangle region )
		{
			regions.Add( guid, region );
		}


		public bool HasRegion ( Guid guid )
		{
			return regions.ContainsKey(guid);
		}


		public Rectangle GetRegion ( Guid guid )
		{
			Rectangle rect;
			if (regions.TryGetValue( guid, out rect ) ) {
				return rect;
			} else {
				Log.Warning("Irradiance map region [{0}] not found", guid );
				return new Rectangle(0,0,0,0);
			}
		}



		public Vector4 GetRegionMadScaleOffset ( Guid guid )
		{
			return GetRegion(guid).GetMadOpScaleOffsetNDC( width, height );
		}


		Color[] colors = new[]
		{
			Color.White,
			Color.Orange,
			Color.Red,
			Color.Blue,
			Color.Magenta,
			Color.Black,
		};


		public void DebugDraw( int x, int y, DebugRender dr )
		{
			x	=	MathUtil.Clamp( x, 0, width-1 );
			y	=	MathUtil.Clamp( y, 0, height-1 );

			var recvPatch	=	new Int2(x,y);

			var indexCount	=	indexmap_cpu[ recvPatch ];
			var offset		=	indexCount >> 8;
			var count		=	indexCount & 0xFF;
			var	begin		=	offset;
			var	end			=	offset + count;

			var origin		=	position_cpu[ recvPatch ];
				
			for (var i = begin; i<end; i++)
			{
				var radPatch	=	new PatchIndex( indices_cpu[ i ] );
				var patchCoord	=	new Int3( radPatch.X, radPatch.Y, radPatch.Mip );

				var pos			=	position_cpu[ patchCoord ];

				dr.DrawLine( origin, pos, colors[ radPatch.Mip ] );
			}
		}
	}
}
