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

namespace Fusion.Engine.Graphics.GI
{
	[RequireShader("radiosity", true)]
	public class Radiosity : RenderComponent
	{
		[ShaderDefine]	const int BlockSizeX = 16;
		[ShaderDefine]	const int BlockSizeY = 16;


		static FXConstantBuffer<RADIOSITY>					regRadiosity		=	new CRegister(0, "Radiosity"		);
		static FXConstantBuffer<ShadowMap.CASCADE_SHADOW>	regCascadeShadow	=	new CRegister(1, "CascadeShadow"	);
		static FXConstantBuffer<GpuData.DIRECT_LIGHT>		regDirectLight		=	new CRegister(2, "DirectLight"		);

		static FXTexture2D<Vector4>							regPosition			=	new TRegister(0, "Position"			);
		static FXTexture2D<Vector4>							regAlbedo			=	new TRegister(1, "Albedo"			);
		static FXTexture2D<Vector4>							regNormal			=	new TRegister(2, "Normal"			);
		static FXTexture2D<Vector4>							regArea				=	new TRegister(3, "Area"				);
		static FXTexture2D<uint>							regIndexMap			=	new TRegister(4, "IndexMap"			);
		static FXBuffer<uint>								regIndices			=	new TRegister(5, "Indices"			);
		static FXTexture2D<uint>							regRadiance			=	new TRegister(6, "Radiance"			);

		static FXRWTexture2D<Vector4>						regRadianceUav		=	new URegister(0, "RadianceUav"	);
		static FXRWTexture2D<Vector4>						regIrradianceR		=	new URegister(1, "IrradianceR"	);
		static FXRWTexture2D<Vector4>						regIrradianceG		=	new URegister(2, "IrradianceG"	);
		static FXRWTexture2D<Vector4>						regIrradianceB		=	new URegister(3, "IrradianceB"	);

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
		}

		[StructLayout(LayoutKind.Sequential, Pack=4, Size=64)]
		struct RADIOSITY
		{
			public uint		RegionX;
			public uint		RegionY;
			public uint		RegionWidth;
			public uint		RegionHeight;

			public float	SkyFactor;
			public float	BounceFactor;
		}


		public RenderTarget2D Radiance		{ get { return radiance; } }
		public RenderTarget2D IrradianceR	{ get { return irradianceR; } }
		public RenderTarget2D IrradianceG	{ get { return irradianceG; } }
		public RenderTarget2D IrradianceB	{ get { return irradianceB; } }


		RenderTarget2D	radiance	;
		RenderTarget2D	irradianceR ;
		RenderTarget2D	irradianceG ;
		RenderTarget2D	irradianceB ;

		ConstantBuffer	cbRadiocity	;
		Ubershader		shader;
		StateFactory	factory;


		public Radiosity( RenderSystem rs ) : base(rs)
		{
		}


		public override void Initialize()
		{
			base.Initialize();

			cbRadiocity	=	new ConstantBuffer( rs.Device, typeof(RADIOSITY) );

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
			SafeDispose( ref irradianceR );
			SafeDispose( ref irradianceG );
			SafeDispose( ref irradianceB );

			radiance	=	new RenderTarget2D( rs.Device, ColorFormat.Rgba16F, width, height, true,  true );
			irradianceR =	new RenderTarget2D( rs.Device, ColorFormat.Rgba16F, width, height, false, true );
			irradianceG =	new RenderTarget2D( rs.Device, ColorFormat.Rgba16F, width, height, false, true );
			irradianceB =	new RenderTarget2D( rs.Device, ColorFormat.Rgba16F, width, height, false, true );
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref cbRadiocity );

				SafeDispose( ref radiance	 );
				SafeDispose( ref irradianceR );
				SafeDispose( ref irradianceG );
				SafeDispose( ref irradianceB );
			}

			base.Dispose( disposing );
		}



		/*-----------------------------------------------------------------------------------------
		 *	Radiosity rendering :
		-----------------------------------------------------------------------------------------*/

		public void Render ( GameTime gameTime )
		{
			if (lightMap==null)
			{
				return;
			}

			using ( new PixEvent( "Radiosity" ) )
			{
				device.ResetStates();

				device.ComputeResources[ regPosition	]	=	lightMap.position	;
				device.ComputeResources[ regAlbedo		]	=	lightMap.albedo		;
				device.ComputeResources[ regNormal		]	=	lightMap.normal		;
				device.ComputeResources[ regArea		]	=	lightMap.area		;
				device.ComputeResources[ regIndexMap	]	=	lightMap.indexMap	;
				device.ComputeResources[ regIndices		]	=	lightMap.indices	;

				device.PipelineState	=	factory[ (int)Flags.LIGHTING ];			
				
				device.SetComputeUnorderedAccess( regRadianceUav, radiance.Surface.UnorderedAccess );
					
				device.Dispatch( new Int2( lightMap.Width, lightMap.Height ), new Int2( BlockSizeX, BlockSizeY ) );
			}
		}




		/*-----------------------------------------------------------------------------------------
		 *	Utils :
		-----------------------------------------------------------------------------------------*/

		public static uint GetLMAddress( Int2 coords, int patchSize )
		{
			if (coords.X<0 || coords.Y<0 || coords.X>=RenderSystem.LightmapSize || coords.Y>=RenderSystem.LightmapSize )
			{
				return 0xFFFFFFFF;
			}

			uint x		= (uint)(coords.X / patchSize) & 0xFFF;
			uint y		= (uint)(coords.Y / patchSize) & 0xFFF;
			uint mip	= (uint)MathUtil.LogBase2( patchSize ) & 0xFF;

			return (mip << 24) | (x << 12) | (y);
		}


		public static uint GetLMIndex( int offset, int count )
		{
			if (offset<0 || offset>=0xFFFFFF) throw new ArgumentOutOfRangeException("0 < offset < 0xFFFFFF");
			if (count <0 || count >=0xFF    ) throw new ArgumentOutOfRangeException("0 < count < 0xFF");
			return ((uint)(offset & 0xFFFFFF) << 8) | (uint)(count & 0xFF);
		}



	}
}
