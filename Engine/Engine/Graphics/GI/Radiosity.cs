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
		[ShaderDefine]	const int BlockSizeX = 16;
		[ShaderDefine]	const int BlockSizeY = 16;

		[ShaderDefine]	const uint LightTypeOmni		=	SceneRenderer.LightTypeOmni;
		[ShaderDefine]	const uint LightTypeSpotShadow	=	SceneRenderer.LightTypeSpotShadow;
		[ShaderDefine]	const uint LightSpotShapeRound	=	SceneRenderer.LightSpotShapeRound;
		[ShaderDefine]	const uint LightSpotShapeSquare	=	SceneRenderer.LightSpotShapeSquare;

		static FXConstantBuffer<GpuData.CAMERA>				regCamera			=	new CRegister( 0, "Camera"		);
		static FXConstantBuffer<RADIOSITY>					regRadiosity		=	new CRegister( 1, "Radiosity"		);
		static FXConstantBuffer<ShadowMap.CASCADE_SHADOW>	regCascadeShadow	=	new CRegister( 2, "CascadeShadow"	);
		static FXConstantBuffer<GpuData.DIRECT_LIGHT>		regDirectLight		=	new CRegister( 3, "DirectLight"		);
																								   
		static FXTexture2D<Vector4>							regPosition			=	new TRegister( 0, "Position"			);
		static FXTexture2D<Vector4>							regAlbedo			=	new TRegister( 1, "Albedo"			);
		static FXTexture2D<Vector4>							regNormal			=	new TRegister( 2, "Normal"			);
		static FXTexture2D<Vector4>							regArea				=	new TRegister( 3, "Area"				);
		static FXTexture2D<uint>							regIndexMap			=	new TRegister( 4, "IndexMap"			);
		static FXBuffer<uint>								regIndices			=	new TRegister( 5, "Indices"			);
		static FXTexture2D<Vector4>							regRadiance			=	new TRegister( 6, "Radiance"			);
		static FXTexture2D<Vector4>							regShadowMap		=	new TRegister( 7, "ShadowMap"		);
		static FXTexture2D<Vector4>							regShadowMask		=	new TRegister( 8, "ShadowMask"		);
		static FXStructuredBuffer<SceneRenderer.LIGHT>		regLights			=	new TRegister( 9, "Lights"			);
		static FXTexture2D<Vector4>							regSky				=	new TRegister(10, "Sky"				);
		static FXTextureCube<Vector4>						regSkyBox			=	new TRegister(11, "SkyBox"			);

		static FXSamplerState								regSamplerLinear	=	new SRegister( 0, "LinearSampler"	);
		static FXSamplerComparisonState						regSamplerShadow	=	new SRegister( 1, "ShadowSampler"	);
																								   
		static FXRWTexture2D<Vector4>						regRadianceUav		=	new URegister( 0, "RadianceUav"		);
		static FXRWTexture2D<Vector4>						relLightmapColor	=	new URegister( 1, "LightmapColor"	);
		static FXRWTexture2D<Vector4>						regLightmapDir		=	new URegister( 2, "LightmapDir"		);

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
		}


		public ShaderResource Radiance		{ get { return radiance; } }
		public ShaderResource LightmapColor	{ get { return lightmapColor; } }
		public ShaderResource LightmapDir	{ get { return lightmapDir; } }


		RenderTarget2D	radiance	;
		RenderTarget2D	tempColor;
		RenderTarget2D	tempDir;
		RenderTarget2D	lightmapColor ;
		RenderTarget2D	lightmapDir ;

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

			SafeDispose( ref radiance		);
			SafeDispose( ref tempColor		);
			SafeDispose( ref tempDir		);
			SafeDispose( ref lightmapColor	);
			SafeDispose( ref lightmapDir	);

			radiance		=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10F,	width, height, true,  true );
			tempColor		=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10F,	width, height, false, true );
			tempDir			=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,		width, height, false, true );
			lightmapColor	=	new RenderTarget2D( rs.Device, ColorFormat.Rg11B10F,	width, height, false, true );
			lightmapDir		=	new RenderTarget2D( rs.Device, ColorFormat.Rgba8,		width, height, false, true );
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref cbRadiosity	);

				SafeDispose( ref radiance		);
				SafeDispose( ref tempColor		);
				SafeDispose( ref tempDir		);
				SafeDispose( ref lightmapColor	);
				SafeDispose( ref lightmapDir	);
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

			using ( new PixEvent( "Radiosity" ) )
			{
				device.ResetStates();

				var radiosity = new RADIOSITY();

				radiosity.SkyFactor			=	SkyFactor;
				radiosity.IndirectFactor	=	IndirectFactor;

				cbRadiosity.SetData( radiosity );

				device.ComputeConstants[ regCamera			]	=	rs.RenderWorld.Camera.CameraData;
				device.ComputeConstants[ regRadiosity		]	=	cbRadiosity;
				device.ComputeConstants[ regCascadeShadow	]	=	rs.LightManager.ShadowMap.GetCascadeShadowConstantBuffer();
				device.ComputeConstants[ regDirectLight		]	=	rs.LightManager.DirectLightData;

				device.ComputeResources[ regPosition		]	=	lightMap.position	;
				device.ComputeResources[ regAlbedo			]	=	lightMap.albedo		;
				device.ComputeResources[ regNormal			]	=	lightMap.normal		;
				device.ComputeResources[ regArea			]	=	lightMap.area		;
				device.ComputeResources[ regIndexMap		]	=	lightMap.indexMap	;
				device.ComputeResources[ regIndices			]	=	lightMap.indices	;

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
					device.SetComputeUnorderedAccess( relLightmapColor,		lightmapColor.Surface.UnorderedAccess );
					device.SetComputeUnorderedAccess( regLightmapDir,		lightmapDir.Surface.UnorderedAccess );
					device.ComputeResources			[ regRadiance	]	=	radiance;

					int width	=	lightMap.Width;
					int height	=	lightMap.Height;

					device.Dispatch( new Int2( lightMap.Width, lightMap.Height ), new Int2( BlockSizeX, BlockSizeY ) );
				}


				using ( new PixEvent( "Bilateral Filter" ) )
				{
					if (!SkipDenoising)
					{
						rs.BilateralFilter.FilterSHL1ByAlpha( lightmapColor	, tempColor	, lightMap.albedo, ColorFactor, AlphaFactor, FalloffFactor );
						rs.BilateralFilter.FilterSHL1ByAlpha( lightmapDir	, tempDir	, lightMap.albedo, ColorFactor, AlphaFactor, FalloffFactor );
						//rs.BilateralFilter.FilterSHL1ByAlpha( irradianceB, tempRadiance, lightMap.albedo, ColorFactor, AlphaFactor, FalloffFactor );
					}
				}


				using ( new PixEvent( "Dilation" ) )
				{
					if (!SkipDilation)
					{
						rs.DilateFilter.DilateByMaskAlpha( tempColor, lightmapColor, lightMap.albedo, 0, 1 );
						tempColor.CopyTo( lightmapColor );

						rs.DilateFilter.DilateByMaskAlpha( tempDir, lightmapDir, lightMap.albedo, 0, 1 );
						tempDir.CopyTo( lightmapDir );
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
