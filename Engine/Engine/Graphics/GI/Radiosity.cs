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
using Fusion.Engine.Graphics.Collections;
using System.Diagnostics;
using System.IO;
using Fusion.Engine.Graphics.Scenes;

namespace Fusion.Engine.Graphics.GI
{
	[RequireShader("radiosity", true)]
	public partial class Radiosity : RenderComponent
	{
		public const int RegionSize = 128;

		[ShaderDefine]	const int TileSize			=	RadiositySettings.TileSize;
		[ShaderDefine]	const int ClusterSize		=	RadiositySettings.ClusterSize;

		static FXConstantBuffer<GpuData.CAMERA>				regCamera			=	new CRegister( 0, "Camera"		);
		static FXConstantBuffer<RADIOSITY>					regRadiosity		=	new CRegister( 1, "Radiosity"		);
		static FXConstantBuffer<ShadowSystem.CASCADE_SHADOW>regCascadeShadow	=	new CRegister( 2, "CascadeShadow"	);
		static FXConstantBuffer<GpuData.DIRECT_LIGHT>		regDirectLight		=	new CRegister( 3, "DirectLight"		);
		static FXConstantBuffer<Plane>						regFrustumPlanes	=	new CRegister( 4,6, "FrustumPlanes"	);
																								   
		static FXTexture2D<Vector4>							regPosition			=	new TRegister( 0, "Position"		);
		static FXTexture2D<Vector4>							regAlbedo			=	new TRegister( 1, "Albedo"			);
		static FXTexture2D<Vector4>							regNormal			=	new TRegister( 2, "Normal"			);
		static FXTexture2D<Vector4>							regBBoxMin			=	new TRegister( 3, "BBoxMin"			);
		static FXTexture2D<Vector4>							regBBoxMax			=	new TRegister( 4, "BBoxMax"			);
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

		[StructLayout(LayoutKind.Sequential, Pack=4, Size=128)]
		struct RADIOSITY
		{
			public Matrix	VoxelToWorld;

			public UInt2	RegionXY;
			public uint		RegionWidth;
			public uint		RegionHeight;

			public float	SkyFactor;
			public float	IndirectFactor;
			public float	SecondBounce;
			public float	ShadowFilter;

			public float	ColorBounce;
			public uint		NumRays;
			public float	WhiteAlbedo;
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

		public Radiosity( RenderSystem rs ) : base(rs)
		{
		}


		public override void Initialize()
		{
			base.Initialize();

			cbRadiosity	=	new ConstantBuffer( rs.Device, typeof(RADIOSITY) );

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
				SafeDispose( ref cbRadiosity	);
			}

			base.Dispose( disposing );
		}

		/*-----------------------------------------------------------------------------------------
		 *	Radiosity baking :
		-----------------------------------------------------------------------------------------*/

		void BakeRadiosity ( RadiositySettings settings, Stream stream )
		{
			var instances	=	rs.RenderWorld.Instances
				.Where( inst => inst.Group.HasFlag( InstanceGroup.Static ) )
				.ToArray();

			var sw = new Stopwatch();
			sw.Start();

			using ( var rasterizer = new LightMapRasterizer( rs, instances, settings ) )
			{
				using ( var gbuffer = rasterizer.RasterizeGBuffer() )
				{
					using ( var rtData = BuildBVHTree(gbuffer, instances) )
					{
						using ( var lightMap = RenderLightmap( rtData, gbuffer, settings ) )
						{
							lightMap.Save( stream );
						}
					}
				}
			}

			sw.Stop();
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


		LightMap RenderLightmap(RayTracer.RTData rtData, LightMapGBuffer gbuffer, RadiositySettings settings )
		{
			var lightVol = rs.RenderWorld.LightSet.LightVolume;
			var lightMap = new LightMap( rs, gbuffer.Size, lightVol.VolumeSize, lightVol.WorldMatrix );

			foreach (var pair in gbuffer.Regions)
			{
				lightMap.Regions.Add( pair.Key, pair.Value );
			}

			for (int i=0; i<settings.NumBounces; i++)
			{
				device.ResetStates();

				SetupShaderResources( rtData, gbuffer, lightMap );
				RenderBounce( rtData, gbuffer, lightMap, settings );
			}

			SetupShaderResources( rtData, gbuffer, lightMap );
			RenderVolume( rtData, gbuffer, lightMap, settings );

			return lightMap;
		}


		void RenderBounce( RayTracer.RTData rtData, LightMapGBuffer gbuffer, LightMap lightMap, RadiositySettings settings )
		{
			var fullRegion = new Rectangle( 0, 0, gbuffer.Width, gbuffer.Height );

			//------------------------------------

			using ( new PixEvent( "Lighting" ) )
			{
				Log.Message("Illuminating...");

				device.SetComputeUnorderedAccess( regRadianceUav, lightMap.radiance.Surface.UnorderedAccess );
				device.ComputeResources			[ regRadiance	]	=	lightMap.irradianceL0;
					
				DispatchRegion( Flags.ILLUMINATE, lightMap, fullRegion, settings );
			}

			int totalRegions = MathUtil.IntDivRoundUp( gbuffer.Width * gbuffer.Height, RegionSize * RegionSize );

			//------------------------------------

			using ( new PixEvent( "Integrate Map" ) )
			{
				Log.Message("Ray-tracing lightmap...");

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

					DispatchRegion( Flags.INTEGRATE2, lightMap, region, settings );

					device.Present(0);
				}
			}

			//------------------------------------

			using ( new PixEvent( "Denoising/Dilation" ) )
			{
				FilterLightmap( lightMap.irradianceL0, lightMap.tempHdr, gbuffer.AlbedoTexture, fullRegion, settings );
				FilterLightmap( lightMap.irradianceL1, lightMap.tempLdr, gbuffer.AlbedoTexture, fullRegion, settings );
				FilterLightmap( lightMap.irradianceL2, lightMap.tempLdr, gbuffer.AlbedoTexture, fullRegion, settings );
				FilterLightmap( lightMap.irradianceL3, lightMap.tempLdr, gbuffer.AlbedoTexture, fullRegion, settings );
			}
		}


		void RenderVolume( RayTracer.RTData rtData, LightMapGBuffer gbuffer, LightMap lightMap, RadiositySettings settings )
		{
			using ( new PixEvent( "Integrate Map" ) )
			{
				Log.Message("Ray-tracing light volume...");

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

				SetupShaderConsts( lightMap, new Rectangle(0,0,1,1), settings );

				device.Dispatch( new Int3( width, height, depth ), new Int3( ClusterSize, ClusterSize, ClusterSize ) );
			}			
		}

		/*-----------------------------------------------------------------------------------------
		 *	Radiosity rendering :
		-----------------------------------------------------------------------------------------*/

		void SetupShaderResources( RayTracer.RTData rtData, LightMapGBuffer gbuffer, LightMap lightMap )
		{
			device.ComputeConstants[ regCamera			]	=	rs.RenderWorld.Camera.CameraData;
			device.ComputeConstants[ regRadiosity		]	=	cbRadiosity;
			device.ComputeConstants[ regCascadeShadow	]	=	rs.ShadowSystem.GetCascadeShadowConstantBuffer();
			device.ComputeConstants[ regDirectLight		]	=	rs.LightManager.DirectLightData;
			device.ComputeConstants[ regFrustumPlanes	]	=	rs.RenderWorld.Camera.FrustumPlanes;

			device.ComputeResources[ regPosition		]	=	gbuffer.PositionTexture	;
			device.ComputeResources[ regAlbedo			]	=	gbuffer.AlbedoTexture	;
			device.ComputeResources[ regNormal			]	=	gbuffer.NormalTexture	;
			device.ComputeResources[ regBBoxMin			]	=	gbuffer.bboxMinTexture	;
			device.ComputeResources[ regBBoxMax			]	=	gbuffer.bboxMaxTexture	;
			device.ComputeResources[ regRadiance		]	=	lightMap.irradianceL0	;

			device.ComputeSamplers[ regSamplerShadow	]	=	SamplerState.ShadowSampler;
			device.ComputeSamplers[ regSamplerLinear	]	=	SamplerState.LinearClamp;

			device.ComputeResources[ regShadowMap		]	=	rs.ShadowSystem.ShadowMap.ShadowTexture;
			device.ComputeResources[ regShadowMask		]	=	rs.ShadowSystem.ShadowMap.ParticleShadowTexture;

			device.ComputeResources[ regLights			]	=	rs.LightManager.LightGrid.RadLtDataGpu;

			device.ComputeResources[ regSkyBox			]	=	rs.Sky.SkyCube;

			device.ComputeResources[ regRtTriangles		]	=	rtData.Primitives;
			device.ComputeResources[ regRtBvhTree		]	=	rtData.BvhTree;
			device.ComputeResources[ regRtLmVerts		]	=	rtData.VertexData;
		}


		void SetupShaderConsts ( LightMap lightMap, Rectangle region, RadiositySettings settings )
		{
			var radiosity = new RADIOSITY();

			int x		=	region.X;
			int y		=	region.Y;
			int width	=	region.Width;
			int height	=	region.Height;

			radiosity.VoxelToWorld		=	lightMap.VoxelToWorld;

			radiosity.RegionXY			=	new UInt2((uint)x, (uint)y);
			radiosity.RegionWidth		=	(uint)width;
			radiosity.RegionHeight		=	(uint)height;

			radiosity.SkyFactor			=	SkyFactor;
			radiosity.IndirectFactor	=	IndirectFactor;
			radiosity.SecondBounce		=	SecondBounce;
			radiosity.ShadowFilter		=	ShadowFilterRadius;

			radiosity.ColorBounce		=	ColorBounce;
			radiosity.NumRays			=	(uint)settings.NumRays;
			radiosity.WhiteAlbedo		=	settings.WhiteDiffuse ? 1.0f : 0.0f;

			cbRadiosity.SetData( radiosity );
		}


		void DispatchRegion( Flags pass, LightMap lightMap, Rectangle region, RadiositySettings settings )
		{
			device.PipelineState	=	factory[(int)pass];
				
			SetupShaderConsts( lightMap, region, settings );

			device.Dispatch( new Int2( region.Width, region.Height ), new Int2( TileSize, TileSize ) );
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



		void FilterLightmap( RenderTarget2D irradiance, RenderTarget2D temp, ShaderResource albedo, Rectangle region, RadiositySettings settings )
		{
			float falloff		=	settings.FilterWeight;
			float lumaWeight	=	settings.FilterWeight;
			float alphaWeight	=	20.0f;

			if (settings.UseDilate)
			{
				rs.DilateFilter.DilateByMaskAlpha( temp, region, irradiance, region, albedo, region, 0, 1 );
			}
			else
			{
				rs.Filter.Copy( temp.Surface, irradiance );
			}

			if (settings.UseBilateral)
			{
				rs.BilateralFilter.FilterSHL1ByAlphaSinglePass( irradiance, region, temp, albedo, region, lumaWeight, alphaWeight, falloff ); 
			}
			else
			{
				rs.Filter.Copy( irradiance.Surface, temp );
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
