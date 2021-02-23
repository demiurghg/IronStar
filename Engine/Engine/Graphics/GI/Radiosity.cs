using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Content;
using Fusion.Core.Configuration;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Core;
using Fusion.Engine.Graphics.Lights;
using Fusion.Core.Shell;
using Fusion.Engine.Graphics.Bvh;
using System.Diagnostics;
using System.IO;
using Fusion.Engine.Graphics.Scenes;

namespace Fusion.Engine.Graphics.GI
{
	[RequireShader("radiosity", true)]
	public partial class Radiosity : RenderComponent
	{
		public const int RegionSize = 64;

		[ShaderDefine]	const int TileSize			=	RadiositySettings.TileSize;
		[ShaderDefine]	const int ClusterSize		=	RadiositySettings.ClusterSize;

		static FXConstantBuffer<GpuData.CAMERA>				regCamera			=	new CRegister( 0, "Camera"		);
		static FXConstantBuffer<RADIOSITY>					regRadiosity		=	new CRegister( 1, "Radiosity"		);
		static FXConstantBuffer<ShadowMap.CASCADE_SHADOW>	regCascadeShadow	=	new CRegister( 2, "CascadeShadow"	);
		static FXConstantBuffer<GpuData.DIRECT_LIGHT>		regDirectLight		=	new CRegister( 3, "DirectLight"		);
		static FXConstantBuffer<Plane>						regFrustumPlanes	=	new CRegister( 4,6, "FrustumPlanes"	);
																								   
		static FXTexture2D<Vector4>							regPosition			=	new TRegister( 0, "Position"		);
		static FXTexture2D<Vector4>							regAlbedo			=	new TRegister( 1, "Albedo"			);
		static FXTexture2D<Vector4>							regNormal			=	new TRegister( 2, "Normal"			);
		static FXTexture2D<Vector4>							regRadiance			=	new TRegister( 7, "Radiance"		);
		static FXTexture2D<Vector4>							regShadowMap		=	new TRegister( 8, "ShadowMap"		);
		static FXTexture2D<Vector4>							regShadowMask		=	new TRegister( 9, "ShadowMask"		);
		static FXStructuredBuffer<SceneRenderer.LIGHT>		regLights			=	new TRegister(10, "Lights"			);
		static FXTextureCube<Vector4>						regSkyBox			=	new TRegister(12, "SkyBox"			);
		static FXStructuredBuffer<RayTracer.TRIANGLE>		regRtTriangles		=	new TRegister(18, "RtTriangles"		);
		static FXStructuredBuffer<RayTracer.BVHNODE>		regRtBvhTree		=	new TRegister(19, "RtBvhTree"		);
		static FXStructuredBuffer<LMVertex>					regRtLmVerts		=	new TRegister(20, "RtLmVerts"		);

		static FXSamplerState								regSamplerLinear	=	new SRegister( 0, "LinearSampler"	);
		static FXSamplerComparisonState						regSamplerShadow	=	new SRegister( 1, "ShadowSampler"	);
											
		[ShaderIfDef("ILLUMINATE,COLLAPSE,DILATE")]	
		static FXRWTexture2D<Vector4>	regRadianceUav		=	new URegister( 0, "RadianceUav"		);

		[ShaderIfDef("INTEGRATE2")] static FXRWTexture2D<Vector4>	regIrradianceL0		=	new URegister( 0, "IrradianceL0"	);
		[ShaderIfDef("INTEGRATE2")] static FXRWTexture2D<Vector4>	regIrradianceL1		=	new URegister( 1, "IrradianceL1"	);
		[ShaderIfDef("INTEGRATE2")] static FXRWTexture2D<Vector4>	regIrradianceL2		=	new URegister( 2, "IrradianceL2"	);
		[ShaderIfDef("INTEGRATE2")] static FXRWTexture2D<Vector4>	regIrradianceL3		=	new URegister( 3, "IrradianceL3"	);

		[ShaderIfDef("INTEGRATE3")] static FXRWTexture3D<Vector4>	regLightVolumeL0	=	new URegister( 0, "LightVolumeL0"	);
		[ShaderIfDef("INTEGRATE3")] static FXRWTexture3D<Vector4>	regLightVolumeL1	=	new URegister( 1, "LightVolumeL1"	);
		[ShaderIfDef("INTEGRATE3")] static FXRWTexture3D<Vector4>	regLightVolumeL2	=	new URegister( 2, "LightVolumeL2"	);
		[ShaderIfDef("INTEGRATE3")] static FXRWTexture3D<Vector4>	regLightVolumeL3	=	new URegister( 3, "LightVolumeL3"	);

		enum Flags 
		{	
			ILLUMINATE	=	0x001,
			DILATE		=	0x002,
			COLLAPSE	=	0x004,
			INTEGRATE2	=	0x008,
			INTEGRATE3	=	0x010,
			DENOISE		=	0x020,
			PASS1		=	0x040,
			PASS2		=	0x080,
			RAYTRACE	=	0x100,
		}

		[StructLayout(LayoutKind.Sequential, Pack=4, Size=64)]
		struct RADIOSITY
		{
			public UInt2	RegionXY;
			public uint		RegionWidth;
			public uint		RegionHeight;

			public float	SkyFactor;
			public float	IndirectFactor;
			public float	SecondBounce;
			public float	ShadowFilter;

			public float	ColorBounce;
		}

		struct LMVertex
		{
			public LMVertex( Vector2 lmCoord ) { LMCoord = lmCoord; }
			public LMVertex( float x, float y ) { LMCoord = new Vector2(x,y); }
			public Vector2 LMCoord;
		}

		/*public ShaderResource Radiance		{ get { return lightMap?.radiance;		} }
		public ShaderResource IrradianceL0	{ get { return lightMap?.irradianceL0;	} }
		public ShaderResource IrradianceL1	{ get { return lightMap?.irradianceL1;	} }
		public ShaderResource IrradianceL2	{ get { return lightMap?.irradianceL2;	} }
		public ShaderResource IrradianceL3	{ get { return lightMap?.irradianceL3;	} }
		public ShaderResource LightVolumeL0	{ get { return lightMap?.lightVolumeL0;	} }
		public ShaderResource LightVolumeL1	{ get { return lightMap?.lightVolumeL1;	 } }
		public ShaderResource LightVolumeL2	{ get { return lightMap?.lightVolumeL2;	 } }
		public ShaderResource LightVolumeL3	{ get { return lightMap?.lightVolumeL3;	 } }  */

		ConstantBuffer	cbRadiosity	;
		Ubershader		shader;
		StateFactory	factory;

		RenderTarget2D	tempHDR0;
		RenderTarget2D	tempLDR0;
		RenderTarget2D	tempHDR1;
		RenderTarget2D	tempLDR1;


		public Radiosity( RenderSystem rs ) : base(rs)
		{
		}


		public override void Initialize()
		{
			base.Initialize();

			cbRadiosity	=	new ConstantBuffer( rs.Device, typeof(RADIOSITY) );

			tempHDR0		=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10, RegionSize, RegionSize, true );
			tempLDR0		=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,   RegionSize, RegionSize, true );
			tempHDR1		=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10, RegionSize, RegionSize, true );
			tempLDR1		=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,   RegionSize, RegionSize, true );

			Game.Invoker.RegisterCommand("bakeLightMap", () => new BakeLightMapCmd(this) );

			LoadContent();

			Game.Reloading += (s,e) => LoadContent();
		}


		public void LoadContent()
		{
			shader	=	Game.Content.Load<Ubershader>("radiosity");
			factory	=	shader.CreateFactory( typeof(Flags) );
		}



		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref tempHDR0 );
				SafeDispose( ref tempLDR0 );
				SafeDispose( ref tempHDR1 );
				SafeDispose( ref tempLDR1 );
				SafeDispose( ref cbRadiosity	);
			}

			base.Dispose( disposing );
		}

		/*-----------------------------------------------------------------------------------------
		 *	Radiosity baking :
		-----------------------------------------------------------------------------------------*/

		void BakeRadiosity ( int numBonces, int numRays, bool useFilter, Stream stream )
		{
			var instances	=	rs.RenderWorld.Instances
				.Where( inst => inst.Group.HasFlag( InstanceGroup.Static ) )
				.ToArray();

			var sw = new Stopwatch();

			using ( var rasterizer = new LightMapRasterizer( rs, instances ) )
			{
				using ( var gbuffer = rasterizer.RasterizeGBuffer() )
				{
					using ( var rtData = BuildBVHTree(gbuffer, instances) )
					{
						using ( var lightMap = RenderLightmap( rtData, gbuffer, numBonces, useFilter ) )
						{
							lightMap.Save( stream );
						}
					}
				}
			}

			Log.Message("Done : {0}", sw.Elapsed);
		}


		class BvhDataProvider : RayTracer.BvhDataProvider<LMVertex, Vector4>
		{
			readonly LightMapGBuffer gbuffer;
			public BvhDataProvider( LightMapGBuffer gbuffer ) { this.gbuffer = gbuffer; }

			public override Vector4 Cache( RenderInstance instance )
			{
				return gbuffer.Regions[ instance.LightMapRegionName ].GetMadOpScaleOffsetNDC( gbuffer.Width, gbuffer.Height );
			}

			public override LMVertex Transform( Vector4 cache, ref MeshVertex vertex )
			{
				float x = vertex.TexCoord1.X * cache.X + cache.Z;
				float y = vertex.TexCoord1.Y * cache.Y + cache.W;
				return new LMVertex(x, y);
			}
		}


		RayTracer.RTData BuildBVHTree(LightMapGBuffer gbuffer, IEnumerable<RenderInstance> instances )
		{
			return RayTracer.BuildAccelerationStructure( rs, instances, new BvhDataProvider(gbuffer) );
		}


		LightMap RenderLightmap(RayTracer.RTData rtData, LightMapGBuffer gbuffer, int numBounces, bool useFilter )
		{
			var lightMap = new LightMap( rs, gbuffer.Size, new Size3(1,1,1) );

			foreach (var pair in gbuffer.Regions)
			{
				lightMap.Regions.Add( pair.Key, pair.Value );
			}

			for (int i=0; i<numBounces; i++)
			{
				device.ResetStates();

				SetupShaderResources( rtData, gbuffer, lightMap );
				RenderBounce( rtData, gbuffer, lightMap, useFilter );
			}

			return lightMap;
		}


		void RenderBounce( RayTracer.RTData rtData, LightMapGBuffer gbuffer, LightMap lightMap, bool useFilter )
		{
			var fullRegion = new Rectangle( 0, 0, gbuffer.Width, gbuffer.Height );

			//------------------------------------

			using ( new PixEvent( "Lighting" ) )
			{
				Log.Message("Illuminating...");

				device.SetComputeUnorderedAccess( regRadianceUav, lightMap.radiance.Surface.UnorderedAccess );
				device.ComputeResources			[ regRadiance	]	=	lightMap.irradianceL0;
					
				DispatchRegion( Flags.ILLUMINATE, fullRegion );
			}

			int totalRegions = MathUtil.IntDivRoundUp( gbuffer.Width * gbuffer.Height, RegionSize * RegionSize );

			//------------------------------------

			using ( new PixEvent( "Integrate Map" ) )
			{
				Log.Message("Ray-tracing...");

				for (int i=0; i<totalRegions; i++)
				{
					Log.Message("...{0}/{1}", i+1, totalRegions);

					var coord  = MortonCode.Decode2((uint)i);
					var region = new Rectangle( coord.X * RegionSize, coord.Y * RegionSize, RegionSize, RegionSize );

					device.SetComputeUnorderedAccess( regRadianceUav,		null );
					device.ComputeResources			[ regRadiance	]	=	lightMap.radiance;

					device.SetComputeUnorderedAccess( regIrradianceL0,		lightMap.irradianceL0.Surface.UnorderedAccess );
					device.SetComputeUnorderedAccess( regIrradianceL1,		lightMap.irradianceL1.Surface.UnorderedAccess );
					device.SetComputeUnorderedAccess( regIrradianceL2,		lightMap.irradianceL2.Surface.UnorderedAccess );
					device.SetComputeUnorderedAccess( regIrradianceL3,		lightMap.irradianceL3.Surface.UnorderedAccess );

					DispatchRegion( Flags.INTEGRATE2, region );

					device.Present(0);
				}
			}

			//------------------------------------

			using ( new PixEvent( "Denoising/Dilation" ) )
			{
				if (useFilter)
				{
					for (int i=0; i<totalRegions; i++)
					{
						var coord  = MortonCode.Decode2((uint)i);
						var region = new Rectangle( coord.X * RegionSize, coord.Y * RegionSize, RegionSize, RegionSize );

						FilterLightmap( lightMap.irradianceL0, tempHDR0, gbuffer.AlbedoTexture, region, WeightIntensitySHL0, 20, FalloffIntensitySHL0 );
						FilterLightmap( lightMap.irradianceL1, tempLDR0, gbuffer.AlbedoTexture, region, WeightDirectionSHL1, 20, FalloffDirectionSHL1 );
						FilterLightmap( lightMap.irradianceL2, tempLDR0, gbuffer.AlbedoTexture, region, WeightDirectionSHL1, 20, FalloffDirectionSHL1 );
						FilterLightmap( lightMap.irradianceL3, tempLDR0, gbuffer.AlbedoTexture, region, WeightDirectionSHL1, 20, FalloffDirectionSHL1 );
					}
				}
			}
		}


		/*-----------------------------------------------------------------------------------------
		 *	Radiosity rendering :
		-----------------------------------------------------------------------------------------*/

		void SetupShaderResources( RayTracer.RTData rtData, LightMapGBuffer gbuffer, LightMap lightMap )
		{
			device.ComputeConstants[ regCamera			]	=	rs.RenderWorld.Camera.CameraData;
			device.ComputeConstants[ regRadiosity		]	=	cbRadiosity;
			device.ComputeConstants[ regCascadeShadow	]	=	rs.LightManager.ShadowMap.GetCascadeShadowConstantBuffer();
			device.ComputeConstants[ regDirectLight		]	=	rs.LightManager.DirectLightData;
			device.ComputeConstants[ regFrustumPlanes	]	=	rs.RenderWorld.Camera.FrustumPlanes;

			device.ComputeResources[ regPosition		]	=	gbuffer.PositionTexture	;
			device.ComputeResources[ regAlbedo			]	=	gbuffer.AlbedoTexture	;
			device.ComputeResources[ regNormal			]	=	gbuffer.NormalTexture	;
			device.ComputeResources[ regRadiance		]	=	lightMap.irradianceL0	;

			device.ComputeSamplers[ regSamplerShadow	]	=	SamplerState.ShadowSampler;
			device.ComputeSamplers[ regSamplerLinear	]	=	SamplerState.LinearClamp;

			device.ComputeResources[ regShadowMap		]	=	rs.LightManager.ShadowMap.ShadowTexture;
			device.ComputeResources[ regShadowMask		]	=	rs.LightManager.ShadowMap.ParticleShadowTexture;

			device.ComputeResources[ regLights			]	=	rs.LightManager.LightGrid.RadLtDataGpu;

			device.ComputeResources[ regSkyBox			]	=	rs.Sky.SkyCubeDiffuse;

			device.ComputeResources[ regRtTriangles		]	=	rtData.Primitives;
			device.ComputeResources[ regRtBvhTree		]	=	rtData.BvhTree;
			device.ComputeResources[ regRtLmVerts		]	=	rtData.VertexData;
		}



		void DispatchRegion( Flags pass, Rectangle region, int mip = 0 )
		{
			device.PipelineState	=	factory[(int)pass];
				
			var radiosity = new RADIOSITY();

			int x		=	region.X >> mip;
			int y		=	region.Y >> mip;
			int width	=	region.Width >> mip;
			int height	=	region.Height >> mip;

			radiosity.RegionXY			=	new UInt2((uint)x, (uint)y);
			radiosity.RegionWidth		=	(uint)width;
			radiosity.RegionHeight		=	(uint)height;

			radiosity.SkyFactor			=	SkyFactor;
			radiosity.IndirectFactor	=	IndirectFactor;
			radiosity.SecondBounce		=	SecondBounce;
			radiosity.ShadowFilter		=	ShadowFilterRadius;

			radiosity.ColorBounce		=	ColorBounce;

			cbRadiosity.SetData( radiosity );

			device.Dispatch( new Int2( width, height ), new Int2( TileSize, TileSize ) );
		}



		/*void RenderRegion( Rectangle region )
		{
			using ( new PixEvent( "Lighting" ) )
			{
				device.SetComputeUnorderedAccess( regRadianceUav, lightMap.radiance.Surface.UnorderedAccess );
					
				DispatchRegion( Flags.ILLUMINATE, region );
			}

			using ( new PixEvent( "Integrate Map" ) )
			{
				device.SetComputeUnorderedAccess( regRadianceUav,		null );
				device.SetComputeUnorderedAccess( regIrradianceL0,		lightMap.irradianceL0.Surface.UnorderedAccess );
				device.SetComputeUnorderedAccess( regIrradianceL1,		lightMap.irradianceL1.Surface.UnorderedAccess );
				device.SetComputeUnorderedAccess( regIrradianceL2,		lightMap.irradianceL2.Surface.UnorderedAccess );
				device.SetComputeUnorderedAccess( regIrradianceL3,		lightMap.irradianceL3.Surface.UnorderedAccess );
				device.ComputeResources			[ regRadiance	]	=	lightMap.radiance;

				DispatchRegion( Flags.INTEGRATE2, region );
			}

			using ( new PixEvent( "Denoising/Dilation" ) )
			{
				FilterLightmap( lightMap.irradianceL0, tempHDR0, lightMap.albedo, region, WeightIntensitySHL0, 20, FalloffIntensitySHL0 );
				FilterLightmap( lightMap.irradianceL1, tempLDR0, lightMap.albedo, region, WeightDirectionSHL1, 20, FalloffDirectionSHL1 );
				FilterLightmap( lightMap.irradianceL2, tempLDR0, lightMap.albedo, region, WeightDirectionSHL1, 20, FalloffDirectionSHL1 );
				FilterLightmap( lightMap.irradianceL3, tempLDR0, lightMap.albedo, region, WeightDirectionSHL1, 20, FalloffDirectionSHL1 );
			}
		}*/



		void IntegrateLightVolume()
		{
			/*using ( new PixEvent( "Integrate Volume" ) )
			{
				device.PipelineState    =   factory[(int)Flags.INTEGRATE3];			

				device.SetComputeUnorderedAccess( regRadianceUav,		null );
				device.SetComputeUnorderedAccess( regLightVolumeL0,		lightMap.lightVolumeL0.UnorderedAccess );
				device.SetComputeUnorderedAccess( regLightVolumeL1,		lightMap.lightVolumeL1.UnorderedAccess );
				device.SetComputeUnorderedAccess( regLightVolumeL2,		lightMap.lightVolumeL2.UnorderedAccess );
				device.SetComputeUnorderedAccess( regLightVolumeL3,		lightMap.lightVolumeL3.UnorderedAccess );
				device.ComputeResources			[ regRadiance	]	=	lightMap.radiance;

				int width	=	lightMap.lightVolumeL0.Width;
				int height	=	lightMap.lightVolumeL0.Height;
				int depth	=	lightMap.lightVolumeL0.Depth;

				device.Dispatch( new Int3( width, height, depth ), new Int3( ClusterSize, ClusterSize, ClusterSize ) );
			}	   */
		}



		void FilterLightmap( RenderTarget2D irradiance, RenderTarget2D temp, ShaderResource albedo, Rectangle region, float lumaFactor, float alphaFactor, float falloff )
		{
			if (!SkipFiltering)
			{
				var tempRegion = new Rectangle( 0,0, RegionSize, RegionSize );
				rs.BilateralFilter.FilterSHL1ByAlphaSinglePass( temp, tempRegion, irradiance, albedo, region, lumaFactor, alphaFactor, falloff ); 
				rs.DilateFilter.DilateByMaskAlpha( irradiance, region, temp, tempRegion, albedo, region, 0, 1 );
			}
		}

		/*-----------------------------------------------------------------------------------------
		 *	Utils :
		-----------------------------------------------------------------------------------------*/

		public static float Falloff( float distance )
		{
			return 1 / (0.0001f + distance + distance);
		}


		public static uint GetLMIndex( int offset, int count )
		{
			if (offset<0 || offset>=0xFFFFFF) throw new ArgumentOutOfRangeException("0 < offset < 0xFFFFFF");
			if (count <0 || count >=0xFF    ) throw new ArgumentOutOfRangeException("0 < count < 0xFF");
			return ((uint)(offset & 0xFFFFFF) << 8) | (uint)(count & 0xFF);
		}



		public static Matrix ComputeWorldToVoxelMatrix( int width, int height, int depth, int stride, Vector3 origin )
		{
			var translation0	=	Matrix.Translation( width / 2.0f * stride, height / 2.0f * stride, depth / 2.0f * stride );
			var translation1	=	Matrix.Translation( origin );

			var scaling0		=	Matrix.Scaling( 1.0f / width, 1.0f / height, 1.0f / depth );
			var scaling1		=	Matrix.Scaling( 1.0f / stride );

			return	translation0 * translation1 * scaling0 * scaling1;
		}


		/*static Vector3 VoxelToWorld( Int3 voxel, LightMap.HeaderData header )
		{
			var result = new Vector4(voxel.X, voxel.Y, voxel.Z, 0) * GetVoxelToWorldScale(header) + GetVoxelToWorldOffset(header);
			return new Vector3( result.X, result.Y, result.Z );
		}


		public Vector4 GetVoxelToWorldScale( LightMap.HeaderData header )
		{
			float s = header.VolumeStride;
			return new Vector4( s, s, s, 0 );
		}


		public Vector4 GetVoxelToWorldOffset( LightMap lightMap )
		{
			var header = lightMap.Header;
			float s = header.VolumeStride;
			float w = header.VolumeSize.Width;
			float h = header.VolumeSize.Height;
			float d = header.VolumeSize.Depth;
			float x = header.VolumePosition.X - (s*w/2) + s/2;
			float y = header.VolumePosition.Y -         + s/2;
			float z = header.VolumePosition.Z - (s*d/2) + s/2;
			return new Vector4( x, y, z, 0 );
		}	  */


		/*public static Vector4 GetVoxelToWorldScale( LightMap lightMap )
		{
			return lightMap==null ? new Vector4(1,1,1,1) : GetVoxelToWorldScale(lightMap.Header);
		}


		public static Vector4 GetVoxelToWorldOffset( LightMap lightMap )
		{
			return lightMap==null ? new Vector4(0,0,0,0) : GetVoxelToWorldOffset(lightMap.Header);
		}


		static Vector4 GetVolumeDimension( LightMap lightMap )
		{
			return lightMap==null ? new Vector4(1,1,1,1) : new Vector4(	lightMap.Header.VolumeSize.Width, lightMap.Header.VolumeSize.Height, lightMap.Header.VolumeSize.Depth, 1 );
		}


		public static Vector4 GetWorldToVoxelScale( LightMap lightMap )
		{
			return Vector4.One / GetVoxelToWorldScale(lightMap) / GetVolumeDimension(lightMap);
		}

		public static Vector4 GetWorldToVoxelOffset( LightMap lightMap )
		{
			return ( (-1) * GetVoxelToWorldOffset(lightMap) / GetVoxelToWorldScale(lightMap) + Vector4.One * 0.5f ) / GetVolumeDimension(lightMap);
		}	  */
	}
}
