﻿using System;
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
		[ShaderDefine]	const int BlockSizeX = RadiositySettings.TileSize;
		[ShaderDefine]	const int BlockSizeY = RadiositySettings.TileSize;

		[ShaderDefine]	const uint LightTypeOmni		=	SceneRenderer.LightTypeOmni;
		[ShaderDefine]	const uint LightTypeSpotShadow	=	SceneRenderer.LightTypeSpotShadow;
		[ShaderDefine]	const uint LightSpotShapeRound	=	SceneRenderer.LightSpotShapeRound;
		[ShaderDefine]	const uint LightSpotShapeSquare	=	SceneRenderer.LightSpotShapeSquare;

		static FXConstantBuffer<GpuData.CAMERA>				regCamera			=	new CRegister( 0, "Camera"		);
		static FXConstantBuffer<RADIOSITY>					regRadiosity		=	new CRegister( 1, "Radiosity"		);
		static FXConstantBuffer<ShadowMap.CASCADE_SHADOW>	regCascadeShadow	=	new CRegister( 2, "CascadeShadow"	);
		static FXConstantBuffer<GpuData.DIRECT_LIGHT>		regDirectLight		=	new CRegister( 3, "DirectLight"		);
																								   
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

		static FXSamplerState								regSamplerLinear	=	new SRegister( 0, "LinearSampler"	);
		static FXSamplerComparisonState						regSamplerShadow	=	new SRegister( 1, "ShadowSampler"	);
																								   
		static FXRWTexture2D<Vector4>						regRadianceUav		=	new URegister( 0, "RadianceUav"		);
		static FXRWTexture2D<Vector4>						regIrradianceL0		=	new URegister( 1, "IrradianceL0"	);
		static FXRWTexture2D<Vector4>						regIrradianceL1		=	new URegister( 2, "IrradianceL1"	);
		static FXRWTexture2D<Vector4>						regIrradianceL2		=	new URegister( 3, "IrradianceL2"	);
		static FXRWTexture2D<Vector4>						regIrradianceL3		=	new URegister( 4, "IrradianceL3"	);

		public LightMap LightMap
		{
			get { return lightMap; }
			set 
			{
				if (lightMap!=value)
				{
					lightMap	=	value;
					fullRefresh	=	true;

					if (lightMap!=null) 
					{
						if (radiance.Width!=lightMap.Width || radiance.Height!=lightMap.Height)
						{
							CreateLightMaps( lightMap.Width, lightMap.Height );
						}
					}
					else
					{
						CreateLightMaps( lightMap.Width, lightMap.Height );
					}
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
			INTEGRATE	=	0x008,
			DENOISE		=	0x010,
			PASS1		=	0x020,
			PASS2		=	0x040,
		}

		[StructLayout(LayoutKind.Sequential, Pack=4, Size=64)]
		struct RADIOSITY
		{
			public uint		RegionX;
			public uint		RegionY;
			public uint		RegionWidth;
			public uint		RegionHeight;

			public float	SkyFactor;
			public float	IndirectFactor;
			public float	SecondBounce;
		}


		public ShaderResource Radiance		{ get { return radiance; } }
		public ShaderResource IrradianceL0	{ get { return irradianceL0; } }
		public ShaderResource IrradianceL1	{ get { return irradianceL1; } }
		public ShaderResource IrradianceL2	{ get { return irradianceL2; } }
		public ShaderResource IrradianceL3	{ get { return irradianceL3; } }


		RenderTarget2D	radiance	;
		RenderTarget2D	tempHDR;
		RenderTarget2D	tempLDR;
		RenderTarget2D	irradianceL0;
		RenderTarget2D	irradianceL1;
		RenderTarget2D	irradianceL2;
		RenderTarget2D	irradianceL3;

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

			CreateLightMaps(16,16);

			LoadContent();

			Game.Reloading += (s,e) => LoadContent();
		}


		public void LoadContent()
		{
			shader	=	Game.Content.Load<Ubershader>("radiosity");
			factory	=	shader.CreateFactory( typeof(Flags) );
		}



		public void CreateLightMaps ( int width, int height )
		{
			Log.Message("Radiosity : created new radiance/irradiance maps : {0}x{1}", width, height );

			SafeDispose( ref radiance	 );
			SafeDispose( ref tempHDR	 );
			SafeDispose( ref tempLDR	 );
			SafeDispose( ref irradianceL0 );
			SafeDispose( ref irradianceL1 );
			SafeDispose( ref irradianceL2 );
			SafeDispose( ref irradianceL3 );

			radiance		=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10,	width, height, true,  true );
			tempHDR			=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10,	width, height, false, true );
			tempLDR			=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,	width, height, false, true );
			irradianceL0	=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10,	width, height, false, true );
			irradianceL1	=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,	width, height, false, true );
			irradianceL2	=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,	width, height, false, true );
			irradianceL3	=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,	width, height, false, true );
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref cbRadiosity	);

				SafeDispose( ref radiance		);
				SafeDispose( ref tempHDR		);
				SafeDispose( ref tempLDR		);
				SafeDispose( ref irradianceL0	);
				SafeDispose( ref irradianceL1	);
				SafeDispose( ref irradianceL2	);
				SafeDispose( ref irradianceL3	);
			}

			base.Dispose( disposing );
		}

		/*-----------------------------------------------------------------------------------------
		 *	Radiosity rendering :
		-----------------------------------------------------------------------------------------*/

		public void Render ( GameTime gameTime )
		{
			if (lightMap==null || lightMap.albedo==null)
			{
				return;
			}

			lightMap.DebugDraw( DebugX, DebugY, rs.RenderWorld.Debug );

			for (int i=0; i<64; i++)
			{
				var n = DecodeDirection(i);
				var u = EncodeDirection(n);
					n = DecodeDirection(u);

				rs.RenderWorld.Debug.DrawLine( Vector3.Zero, n * 20, Color.Orange );
			}


			using ( new PixEvent( "Radiosity" ) )
			{
				device.ResetStates();

				var radiosity = new RADIOSITY();

				radiosity.SkyFactor			=	SkyFactor;
				radiosity.IndirectFactor	=	IndirectFactor;
				radiosity.SecondBounce		=	SecondBounce;

				cbRadiosity.SetData( radiosity );

				device.ComputeConstants[ regCamera			]	=	rs.RenderWorld.Camera.CameraData;
				device.ComputeConstants[ regRadiosity		]	=	cbRadiosity;
				device.ComputeConstants[ regCascadeShadow	]	=	rs.LightManager.ShadowMap.GetCascadeShadowConstantBuffer();
				device.ComputeConstants[ regDirectLight		]	=	rs.LightManager.DirectLightData;

				device.ComputeResources[ regPosition		]	=	lightMap.position	;
				device.ComputeResources[ regAlbedo			]	=	lightMap.albedo		;
				device.ComputeResources[ regNormal			]	=	lightMap.normal		;
				device.ComputeResources[ regTiles			]	=	lightMap.tiles		;
				device.ComputeResources[ regIndexMap		]	=	lightMap.indexMap	;
				device.ComputeResources[ regIndices			]	=	lightMap.indices	;
				device.ComputeResources[ regCache			]	=	lightMap.cache		;
				device.ComputeResources[ regRadiance		]	=	irradianceL0		;

				device.ComputeSamplers[ regSamplerShadow	]	=	SamplerState.ShadowSampler;
				device.ComputeSamplers[ regSamplerLinear	]	=	SamplerState.LinearClamp;

				device.ComputeResources[ regShadowMap		]	=	rs.LightManager.ShadowMap.ShadowTexture;
				device.ComputeResources[ regShadowMask		]	=	rs.LightManager.ShadowMap.ParticleShadowTexture;

				device.ComputeResources[ regSkyBox			]	=	rs.Sky.SkyCube;
				device.ComputeResources[ regSky				]	=	lightMap.sky;


				using ( new PixEvent( "Lighting" ) )
				{
					device.PipelineState    =   factory[(int)Flags.LIGHTING];			
				
					device.SetComputeUnorderedAccess( regRadianceUav, radiance.Surface.UnorderedAccess );
					
					device.Dispatch( new Int2( lightMap.Width, lightMap.Height ), new Int2( BlockSizeX, BlockSizeY ) );
				}


				using ( new PixEvent( "Collapse" ) )
				{
					device.PipelineState    =   factory[(int)Flags.COLLAPSE];			

					for (int mip=1; mip<RadiositySettings.MapPatchLevels; mip++)
					{
						device.SetComputeUnorderedAccess( regRadianceUav,		radiance.GetSurface( mip ).UnorderedAccess );
						device.ComputeResources			[ regRadiance	]	=	radiance.GetShaderResource( mip - 1 );

						int width	=	lightMap.Width  >> mip;
						int height	=	lightMap.Height >> mip;

						device.Dispatch( new Int2( width, height ), new Int2( BlockSizeX, BlockSizeY ) );
					}
				}


				using ( new PixEvent( "Integrate" ) )
				{
					device.PipelineState    =   factory[(int)Flags.INTEGRATE];			

					device.SetComputeUnorderedAccess( regRadianceUav,		null );
					device.SetComputeUnorderedAccess( regIrradianceL0,		irradianceL0.Surface.UnorderedAccess );
					device.SetComputeUnorderedAccess( regIrradianceL1,		irradianceL1.Surface.UnorderedAccess );
					device.SetComputeUnorderedAccess( regIrradianceL2,		irradianceL2.Surface.UnorderedAccess );
					device.SetComputeUnorderedAccess( regIrradianceL3,		irradianceL3.Surface.UnorderedAccess );
					device.ComputeResources			[ regRadiance	]	=	radiance;

					int width	=	lightMap.Width;
					int height	=	lightMap.Height;

					device.Dispatch( new Int2( lightMap.Width, lightMap.Height ), new Int2( BlockSizeX, BlockSizeY ) );
				}


				using ( new PixEvent( "Bilateral Filter" ) )
				{
					if (!SkipDenoising)
					{
						rs.BilateralFilter.FilterSHL1ByAlpha( irradianceL0, tempHDR, lightMap.albedo, ColorFactor, AlphaFactor, FalloffFactor );
						rs.BilateralFilter.FilterSHL1ByAlpha( irradianceL1, tempHDR, lightMap.albedo, ColorFactor, AlphaFactor, FalloffFactor );
						rs.BilateralFilter.FilterSHL1ByAlpha( irradianceL2, tempHDR, lightMap.albedo, ColorFactor, AlphaFactor, FalloffFactor );
						rs.BilateralFilter.FilterSHL1ByAlpha( irradianceL3, tempHDR, lightMap.albedo, ColorFactor, AlphaFactor, FalloffFactor );
					}
				}

				using ( new PixEvent( "Dilation" ) )
				{
					if (!SkipDilation)
					{
						rs.DilateFilter.DilateByMaskAlpha( tempHDR, irradianceL0, lightMap.albedo, 0, 1 );		tempHDR.CopyTo( irradianceL0 );
						rs.DilateFilter.DilateByMaskAlpha( tempLDR, irradianceL1, lightMap.albedo, 0, 1 );		tempLDR.CopyTo( irradianceL1 );
						rs.DilateFilter.DilateByMaskAlpha( tempLDR, irradianceL2, lightMap.albedo, 0, 1 );		tempLDR.CopyTo( irradianceL2 );
						rs.DilateFilter.DilateByMaskAlpha( tempLDR, irradianceL3, lightMap.albedo, 0, 1 );		tempLDR.CopyTo( irradianceL3 );
					}
				}
			}
		}


		/*-----------------------------------------------------------------------------------------
		 *	Utils :
		-----------------------------------------------------------------------------------------*/

		public static float Falloff( float distance )
		{
			return 1 / (0.0001f + distance + distance);
		}

		public static string DecodeLMAddressDebug( uint lmAddr )
		{
			if (lmAddr==0xFFFFFFFF) return "------------------------";
			uint 	lmMip		=	(lmAddr >> 24) & 0xFF;
			uint 	lmX			=	(lmAddr >> 12) & 0xFFF;
			uint 	lmY			=	(lmAddr >>  0) & 0xFFF;
			return string.Format("{0,2} [{1,4} {2,4}]", lmMip, lmX, lmY );
		}

		public static uint EncodeLMAddress( Int3 coords )
		{
			if (coords.X<0 || coords.Y<0 || coords.X>=RenderSystem.LightmapSize || coords.Y>=RenderSystem.LightmapSize )
			{
				return 0xFFFFFFFF;
			}

			uint x		= (uint)(coords.X) & 0xFFF;
			uint y		= (uint)(coords.Y) & 0xFFF;
			uint mip	= (uint)(coords.Z);

			return (mip << 24) | (x << 12) | (y);
		}


		public static Int3 DecodeLMAddress( uint index )
		{
			if (index==0xFFFFFFFF) return new Int3(-1,-1,-1);

			uint 	lmMip		=	(index >> 24) & 0xFF;
			uint 	lmX			=	(index >> 12) & 0xFFF;
			uint 	lmY			=	(index >>  0) & 0xFFF;

			return new Int3( (int)lmX, (int)lmY, (int)lmMip );
		}


		public static uint GetLMIndex( int offset, int count )
		{
			if (offset<0 || offset>=0xFFFFFF) throw new ArgumentOutOfRangeException("0 < offset < 0xFFFFFF");
			if (count <0 || count >=0xFF    ) throw new ArgumentOutOfRangeException("0 < count < 0xFF");
			return ((uint)(offset & 0xFFFFFF) << 8) | (uint)(count & 0xFF);
		}



	}
}
