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

namespace Fusion.Engine.Graphics.GI
{
	[RequireShader("radiosity", true)]
	public partial class Radiosity : RenderComponent
	{
		[ShaderDefine]	const int TileSize			=	RadiositySettings.TileSize;
		[ShaderDefine]	const int ClusterSize		=	RadiositySettings.ClusterSize;
		[ShaderDefine]	const uint PatchCacheSize	=	RadiositySettings.MaxPatchesPerTile;

		[ShaderDefine]	const uint LightTypeOmni		=	SceneRenderer.LightTypeOmni;
		[ShaderDefine]	const uint LightTypeSpotShadow	=	SceneRenderer.LightTypeSpotShadow;
		[ShaderDefine]	const uint LightSpotShapeRound	=	SceneRenderer.LightSpotShapeRound;
		[ShaderDefine]	const uint LightSpotShapeSquare	=	SceneRenderer.LightSpotShapeSquare;

		static FXConstantBuffer<GpuData.CAMERA>				regCamera			=	new CRegister( 0, "Camera"		);
		static FXConstantBuffer<RADIOSITY>					regRadiosity		=	new CRegister( 1, "Radiosity"		);
		static FXConstantBuffer<ShadowMap.CASCADE_SHADOW>	regCascadeShadow	=	new CRegister( 2, "CascadeShadow"	);
		static FXConstantBuffer<GpuData.DIRECT_LIGHT>		regDirectLight		=	new CRegister( 3, "DirectLight"		);
		static FXConstantBuffer<Plane>						regFrustumPlanes	=	new CRegister( 4,6, "FrustumPlanes"	);
																								   
		static FXTexture2D<Vector4>							regPosition			=	new TRegister( 0, "Position"		);
		static FXTexture2D<Vector4>							regAlbedo			=	new TRegister( 1, "Albedo"			);
		static FXTexture2D<Vector4>							regNormal			=	new TRegister( 2, "Normal"			);
		static FXTexture2D<UInt2>							regTiles			=	new TRegister( 3, "Tiles"			);
		static FXTexture2D<uint>							regIndexMap			=	new TRegister( 4, "IndexMap"		);
		static FXBuffer<uint>								regIndices			=	new TRegister( 5, "Indices"			);
		static FXBuffer<uint>								regCache			=	new TRegister( 6, "Cache"			);
		static FXTexture2D<Vector4>							regRadiance			=	new TRegister( 7, "Radiance"		);
		static FXTexture2D<Vector4>							regShadowMap		=	new TRegister( 8, "ShadowMap"		);
		static FXTexture2D<Vector4>							regShadowMask		=	new TRegister( 9, "ShadowMask"		);
		static FXStructuredBuffer<SceneRenderer.LIGHT>		regLights			=	new TRegister(10, "Lights"			);
		static FXTexture2D<Vector4>							regSky				=	new TRegister(11, "Sky"				);
		static FXTextureCube<Vector4>						regSkyBox			=	new TRegister(12, "SkyBox"			);
		static FXTexture2D<Vector4>							regBBoxMin			=	new TRegister(13, "BBoxMin"			);
		static FXTexture2D<Vector4>							regBBoxMax			=	new TRegister(14, "BBoxMax"			);
		static FXTexture3D<UInt2>							regClusters			=	new TRegister(15, "Clusters"		);
		static FXTexture3D<uint>							regIndexVolume		=	new TRegister(16, "IndexVolume"		);
		static FXTexture3D<Vector4>							regSkyVolume		=	new TRegister(17, "SkyVolume"		);

		static FXSamplerState								regSamplerLinear	=	new SRegister( 0, "LinearSampler"	);
		static FXSamplerComparisonState						regSamplerShadow	=	new SRegister( 1, "ShadowSampler"	);
											
		[ShaderIfDef("LIGHTING,COLLAPSE,DILATE")]	
		static FXRWTexture2D<Vector4>	regRadianceUav		=	new URegister( 0, "RadianceUav"		);

		[ShaderIfDef("INTEGRATE2")] static FXRWTexture2D<Vector4>	regIrradianceL0		=	new URegister( 0, "IrradianceL0"	);
		[ShaderIfDef("INTEGRATE2")] static FXRWTexture2D<Vector4>	regIrradianceL1		=	new URegister( 1, "IrradianceL1"	);
		[ShaderIfDef("INTEGRATE2")] static FXRWTexture2D<Vector4>	regIrradianceL2		=	new URegister( 2, "IrradianceL2"	);
		[ShaderIfDef("INTEGRATE2")] static FXRWTexture2D<Vector4>	regIrradianceL3		=	new URegister( 3, "IrradianceL3"	);

		[ShaderIfDef("INTEGRATE3")] static FXRWTexture3D<Vector4>	regLightVolumeL0	=	new URegister( 0, "LightVolumeL0"	);
		[ShaderIfDef("INTEGRATE3")] static FXRWTexture3D<Vector4>	regLightVolumeL1	=	new URegister( 1, "LightVolumeL1"	);
		[ShaderIfDef("INTEGRATE3")] static FXRWTexture3D<Vector4>	regLightVolumeL2	=	new URegister( 2, "LightVolumeL2"	);
		[ShaderIfDef("INTEGRATE3")] static FXRWTexture3D<Vector4>	regLightVolumeL3	=	new URegister( 3, "LightVolumeL3"	);

		public LightMap LightMap
		{
			get { return lightMap; }
			set {
				if (lightMap!=value)
				{
					lightMap	=	value;
					fullRefresh	=	true;
				}
			}
		}

		bool fullRefresh = false;
		LightMap lightMap;
		

		enum Flags 
		{	
			LIGHTING	=	0x001,
			DILATE		=	0x002,
			COLLAPSE	=	0x004,
			INTEGRATE2	=	0x008,
			INTEGRATE3	=	0x010,
			DENOISE		=	0x020,
			PASS1		=	0x040,
			PASS2		=	0x080,
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
		}

		public ShaderResource Radiance		{ get { return lightMap?.radiance;		} }
		public ShaderResource IrradianceL0	{ get { return lightMap?.irradianceL0;	} }
		public ShaderResource IrradianceL1	{ get { return lightMap?.irradianceL1;	} }
		public ShaderResource IrradianceL2	{ get { return lightMap?.irradianceL2;	} }
		public ShaderResource IrradianceL3	{ get { return lightMap?.irradianceL3;	} }
		public ShaderResource LightVolumeL0	{ get { return lightMap?.lightVolumeL0;	} }
		public ShaderResource LightVolumeL1	{ get { return lightMap?.lightVolumeL1;	 } }
		public ShaderResource LightVolumeL2	{ get { return lightMap?.lightVolumeL2;	 } }
		public ShaderResource LightVolumeL3	{ get { return lightMap?.lightVolumeL3;	 } }

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
		 *	Radiosity rendering :
		-----------------------------------------------------------------------------------------*/

		int counter = 0;

		public void Render ( GameTime gameTime )
		{
			if (lightMap==null || lightMap.albedo==null)
			{
				return;
			}

			device.ResetStates();

			using ( new PixEvent( "Radiosity" ) )
			{
				SetupShaderResources();

				int regSize =	256;
				int regX	=	lightMap.Width  / regSize;
				int regY	=	lightMap.Height / regSize;
				int x		=	counter % regX * regSize;
				int y		=	counter / regX * regSize;
				int w		=	regSize;
				int h		=	regSize;

				counter		=	(counter + 1) % (regX * regY);

				RenderRegion( new Rectangle(x, y, w, h) );
				//RenderRegion( new Rectangle(0,0, lightMap.Width, lightMap.Height) );

				IntegrateLightVolume();
			}
		}


		
		void SetupShaderResources()
		{
			device.ComputeConstants[ regCamera			]	=	rs.RenderWorld.Camera.CameraData;
			device.ComputeConstants[ regRadiosity		]	=	cbRadiosity;
			device.ComputeConstants[ regCascadeShadow	]	=	rs.LightManager.ShadowMap.GetCascadeShadowConstantBuffer();
			device.ComputeConstants[ regDirectLight		]	=	rs.LightManager.DirectLightData;
			device.ComputeConstants[ regFrustumPlanes	]	=	rs.RenderWorld.Camera.FrustumPlanes;

			device.ComputeResources[ regPosition		]	=	lightMap.position	;
			device.ComputeResources[ regAlbedo			]	=	lightMap.albedo		;
			device.ComputeResources[ regNormal			]	=	lightMap.normal		;
			device.ComputeResources[ regTiles			]	=	lightMap.tiles		;
			device.ComputeResources[ regIndexMap		]	=	lightMap.indexMap	;
			device.ComputeResources[ regIndices			]	=	lightMap.indices	;
			device.ComputeResources[ regCache			]	=	lightMap.cache		;
			device.ComputeResources[ regRadiance		]	=	lightMap.irradianceL0		;

			device.ComputeSamplers[ regSamplerShadow	]	=	SamplerState.ShadowSampler;
			device.ComputeSamplers[ regSamplerLinear	]	=	SamplerState.LinearClamp;

			device.ComputeResources[ regShadowMap		]	=	rs.LightManager.ShadowMap.ShadowTexture;
			device.ComputeResources[ regShadowMask		]	=	rs.LightManager.ShadowMap.ParticleShadowTexture;

			device.ComputeResources[ regSkyBox			]	=	rs.Sky.SkyCube;
			device.ComputeResources[ regSky				]	=	lightMap.sky;

			device.ComputeResources[ regBBoxMin			]	=	lightMap.bboxMin;
			device.ComputeResources[ regBBoxMax			]	=	lightMap.bboxMax;

			device.ComputeResources[ regClusters		]	=	lightMap.clusters;
			device.ComputeResources[ regIndexVolume		]	=	lightMap.indexVol;
			device.ComputeResources[ regSkyVolume		]	=	lightMap.skyVol;
		}



		void DispatchRegion( Rectangle region, int mip = 0 )
		{
			var radiosity = new RADIOSITY();

			int x		=	region.X >> mip;
			int y		=	region.Y >> mip;
			int width	=	region.Width >> mip;
			int height	=	region.Height >> mip;

			radiosity.RegionXY			=	new UInt2((uint)x, (uint)y);
			radiosity.RegionWidth		=	(uint)width;
			radiosity.RegionHeight		=	(uint)height;

			radiosity.SkyFactor			=	SkyFactor;
			radiosity.IndirectFactor	=	IndirectFactor / lightMap.Header.LightMapSampleCount;
			radiosity.SecondBounce		=	SecondBounce;
			radiosity.ShadowFilter		=	ShadowFilterRadius;

			cbRadiosity.SetData( radiosity );

			device.Dispatch( new Int2( width, height ), new Int2( TileSize, TileSize ) );
		}



		void RenderRegion( Rectangle region )
		{
			using ( new PixEvent( "Lighting" ) )
			{
				device.PipelineState    =   factory[(int)Flags.LIGHTING];			
				
				device.SetComputeUnorderedAccess( regRadianceUav, lightMap.radiance.Surface.UnorderedAccess );
					
				DispatchRegion( region );
			}

			using ( new PixEvent( "Collapse" ) )
			{
				device.PipelineState    =   factory[(int)Flags.COLLAPSE];			

				for (int mip=1; mip<RadiositySettings.MapPatchLevels; mip++)
				{
					device.SetComputeUnorderedAccess( regRadianceUav,		lightMap.radiance.GetSurface( mip ).UnorderedAccess );
					device.ComputeResources			[ regRadiance	]	=	lightMap.radiance.GetShaderResource( mip - 1 );

					DispatchRegion( region, mip );
				}
			}

			using ( new PixEvent( "Integrate Map" ) )
			{
				device.PipelineState    =   factory[(int)Flags.INTEGRATE2];			

				device.SetComputeUnorderedAccess( regRadianceUav,		null );
				device.SetComputeUnorderedAccess( regIrradianceL0,		lightMap.irradianceL0.Surface.UnorderedAccess );
				device.SetComputeUnorderedAccess( regIrradianceL1,		lightMap.irradianceL1.Surface.UnorderedAccess );
				device.SetComputeUnorderedAccess( regIrradianceL2,		lightMap.irradianceL2.Surface.UnorderedAccess );
				device.SetComputeUnorderedAccess( regIrradianceL3,		lightMap.irradianceL3.Surface.UnorderedAccess );
				device.ComputeResources			[ regRadiance	]	=	lightMap.radiance;

				DispatchRegion( region );
			}

			using ( new PixEvent( "Denoising/Dilation" ) )
			{
				FilterLightmap( lightMap.irradianceL0, lightMap.tempHDR, lightMap.albedo, WeightIntensitySHL0, 20, FalloffIntensitySHL0 );
				FilterLightmap( lightMap.irradianceL1, lightMap.tempLDR, lightMap.albedo, WeightDirectionSHL1, 20, FalloffDirectionSHL1 );
				FilterLightmap( lightMap.irradianceL2, lightMap.tempLDR, lightMap.albedo, WeightDirectionSHL1, 20, FalloffDirectionSHL1 );
				FilterLightmap( lightMap.irradianceL3, lightMap.tempLDR, lightMap.albedo, WeightDirectionSHL1, 20, FalloffDirectionSHL1 );
			}
		}



		void IntegrateLightVolume()
		{
			using ( new PixEvent( "Integrate Volume" ) )
			{
				device.PipelineState    =   factory[(int)Flags.INTEGRATE3];			

				device.SetComputeUnorderedAccess( regRadianceUav,		null );
				device.SetComputeUnorderedAccess( regLightVolumeL0,		lightMap.lightVolumeL0.UnorderedAccess );
				device.SetComputeUnorderedAccess( regLightVolumeL1,		lightMap.lightVolumeL1.UnorderedAccess );
				device.SetComputeUnorderedAccess( regLightVolumeL2,		lightMap.lightVolumeL2.UnorderedAccess );
				device.SetComputeUnorderedAccess( regLightVolumeL3,		lightMap.lightVolumeL3.UnorderedAccess );
				device.ComputeResources			[ regRadiance	]	=	lightMap.radiance;

				int width	=	lightMap.indexVol.Width;
				int height	=	lightMap.indexVol.Height;
				int depth	=	lightMap.indexVol.Depth;

				device.Dispatch( new Int3( width, height, depth ), new Int3( ClusterSize, ClusterSize, ClusterSize ) );
			}
		}



		void FilterLightmap( RenderTarget2D irradiance, RenderTarget2D temp, ShaderResource albedo, float lumaFactor, float alphaFactor, float falloff )
		{
			if (!SkipDenoising)
			{
				rs.BilateralFilter.FilterSHL1ByAlphaSinglePass( temp, irradiance, albedo, lumaFactor, alphaFactor, falloff ); 
			}
			else 
			{
				irradiance.CopyTo( temp );
			}

			if (!SkipDilation)
			{
				rs.DilateFilter.DilateByMaskAlpha( irradiance, temp, albedo, 0, 1 );
			}
			else
			{
				temp.CopyTo( irradiance );
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


		public static Vector3 VoxelToWorld( Int3 voxel, FormFactor.Header header )
		{
			var result = new Vector4(voxel.X, voxel.Y, voxel.Z, 0) * GetVoxelToWorldScale(header) + GetVoxelToWorldOffset(header);
			return new Vector3( result.X, result.Y, result.Z );
		}


		static public Vector4 GetVoxelToWorldScale( FormFactor.Header header )
		{
			float s = header.VolumeStride;
			return new Vector4( s, s, s, 0 );
		}


		static public Vector4 GetVoxelToWorldOffset( FormFactor.Header header )
		{
			float s = header.VolumeStride;
			float w = header.VolumeWidth;
			float h = header.VolumeHeight;
			float d = header.VolumeDepth;
			float x = header.VolumePosition.X - (s*w/2) + s/2;
			float y = header.VolumePosition.Y -         + s/2;
			float z = header.VolumePosition.Z - (s*d/2) + s/2;
			return new Vector4( x, y, z, 0 );
		}


		public Vector4 GetVoxelToWorldScale()
		{
			return lightMap==null ? new Vector4(1,1,1,1) : GetVoxelToWorldScale(lightMap.Header);
		}


		public Vector4 GetVoxelToWorldOffset()
		{
			return lightMap==null ? new Vector4(0,0,0,0) : GetVoxelToWorldOffset(lightMap.Header);
		}


		Vector4 GetVolumeDimension()
		{
			return lightMap==null ? new Vector4(1,1,1,1) : new Vector4(	lightMap.Header.VolumeWidth, lightMap.Header.VolumeHeight, lightMap.Header.VolumeDepth, 1 );
		}


		public Vector4 GetWorldToVoxelScale()
		{
			return Vector4.One / GetVoxelToWorldScale() / GetVolumeDimension();
		}

		public Vector4 GetWorldToVoxelOffset()
		{
			return ( (-1) * GetVoxelToWorldOffset() / GetVoxelToWorldScale() + Vector4.One * 0.5f ) / GetVolumeDimension();
		}
	}
}
