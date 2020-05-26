using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using System.IO;
using Fusion.Engine.Graphics.Ubershaders;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using Native.Embree;
using Fusion.Engine.Graphics.Lights;
using Fusion.Core.Shell;

namespace Fusion.Engine.Graphics {

	[RequireShader("cubegen", true)]
	internal partial class CubeMapFilter : RenderComponent 
	{
		static FXConstantBuffer<Vector4> regFilterData = new CRegister( 0, 20*7, "FilterData" );

		static FXSamplerState				regLinearSampler	= 	new SRegister(0, "LinearSampler");
		static FXTextureCube<Vector4>		regSource			=	new TRegister(0, "Source"		);

		[ShaderIfDef("DOWNSAMPLE")]	static FXRWTexture2DArray<Vector4>	regTarget	= 	new URegister(0, "Target"	); 
									 		
		[ShaderIfDef("PREFILTER")]	static FXRWTexture2DArray<Vector4>	regTarget0	= 	new URegister(0, "Target0"	); 
		[ShaderIfDef("PREFILTER")]	static FXRWTexture2DArray<Vector4>	regTarget1	= 	new URegister(1, "Target1"	); 
		[ShaderIfDef("PREFILTER")]	static FXRWTexture2DArray<Vector4>	regTarget2	= 	new URegister(2, "Target2"	); 
		[ShaderIfDef("PREFILTER")]	static FXRWTexture2DArray<Vector4>	regTarget3	= 	new URegister(3, "Target3"	); 
		[ShaderIfDef("PREFILTER")]	static FXRWTexture2DArray<Vector4>	regTarget4	= 	new URegister(4, "Target4"	); 
		[ShaderIfDef("PREFILTER")]	static FXRWTexture2DArray<Vector4>	regTarget5	= 	new URegister(5, "Target5"	); 
		[ShaderIfDef("PREFILTER")]	static FXRWTexture2DArray<Vector4>	regTarget6	= 	new URegister(6, "Target6"	); 

		[ShaderDefine]
		const int BlockSize = 8;

		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=64)]
		struct PARAMS {
			public	float	Roughness;
			public	float	MipLevel;
			public	float	TargetSize;
			public	float	SourceSize;
		}

		Ubershader		shader;
		StateFactory	factory;

		enum Flags {
			DOWNSAMPLE	=	0x0001,
			PREFILTER	=	0x0002,
			REFERENCE	=	0x0004,
			DIFFERENCE	=	0x0008,
		}
		

		[AECategory("General")] public bool UseReference { get; set; } = false;
		[AECategory("General")] public bool UseDifference { get; set; } = false;

		[Config] [AECategory("MipLevel0")] [AEValueRange(-12, 0, 1.0f, 0.1f)] public float Mip0_Weight1  { get; set; } =  -9.0f;
		[Config] [AECategory("MipLevel0")] [AEValueRange(-12, 0, 1.0f, 0.1f)] public float Mip0_Weight2  { get; set; } = -12.0f;
		[Config] [AECategory("MipLevel0")] [AEValueRange(  0,10, 0.5f, 0.1f)] public float Mip0_Radius1  { get; set; } =   1.3f;
		[Config] [AECategory("MipLevel0")] [AEValueRange(  0,10, 0.5f, 0.1f)] public float Mip0_Radius2  { get; set; } =   2.0f;
		[Config] [AECategory("MipLevel0")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip0_LodBias0 { get; set; } =   0.0f;
		[Config] [AECategory("MipLevel0")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip0_LodBias1 { get; set; } =   0.5f;
		[Config] [AECategory("MipLevel0")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip0_LodBias2 { get; set; } =   0.0f;

		[Config] [AECategory("MipLevel1")] [AEValueRange(-12, 0, 1.0f, 0.1f)] public float Mip1_Weight1  { get; set; } =  -3.0f;
		[Config] [AECategory("MipLevel1")] [AEValueRange(-12, 0, 1.0f, 0.1f)] public float Mip1_Weight2  { get; set; } =  -5.0f;
		[Config] [AECategory("MipLevel1")] [AEValueRange(  0,10, 0.5f, 0.1f)] public float Mip1_Radius1  { get; set; } =   2.0f;
		[Config] [AECategory("MipLevel1")] [AEValueRange(  0,10, 0.5f, 0.1f)] public float Mip1_Radius2  { get; set; } =   6.5f;
		[Config] [AECategory("MipLevel1")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip1_LodBias0 { get; set; } =   0.8f;
		[Config] [AECategory("MipLevel1")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip1_LodBias1 { get; set; } =   1.5f;
		[Config] [AECategory("MipLevel1")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip1_LodBias2 { get; set; } =   2.5f;

		[Config] [AECategory("MipLevel2")] [AEValueRange(-12, 0, 1.0f, 0.1f)] public float Mip2_Weight1  { get; set; } =  -2.0f;
		[Config] [AECategory("MipLevel2")] [AEValueRange(-12, 0, 1.0f, 0.1f)] public float Mip2_Weight2  { get; set; } =  -4.3f;
		[Config] [AECategory("MipLevel2")] [AEValueRange(  0,10, 0.5f, 0.1f)] public float Mip2_Radius1  { get; set; } =   3.5f;
		[Config] [AECategory("MipLevel2")] [AEValueRange(  0,10, 0.5f, 0.1f)] public float Mip2_Radius2  { get; set; } =   4.5f;
		[Config] [AECategory("MipLevel2")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip2_LodBias0 { get; set; } =   1.0f;
		[Config] [AECategory("MipLevel2")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip2_LodBias1 { get; set; } =   1.5f;
		[Config] [AECategory("MipLevel2")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip2_LodBias2 { get; set; } =   3.5f;

		[Config] [AECategory("MipLevel3")] [AEValueRange(-12, 0, 1.0f, 0.1f)] public float Mip3_Weight1  { get; set; } = -2;
		[Config] [AECategory("MipLevel3")] [AEValueRange(-12, 0, 1.0f, 0.1f)] public float Mip3_Weight2  { get; set; } = -2;
		[Config] [AECategory("MipLevel3")] [AEValueRange(  0,10, 0.5f, 0.1f)] public float Mip3_Radius1  { get; set; } = 3.5f;
		[Config] [AECategory("MipLevel3")] [AEValueRange(  0,10, 0.5f, 0.1f)] public float Mip3_Radius2  { get; set; } = 6.5f;
		[Config] [AECategory("MipLevel3")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip3_LodBias0 { get; set; } = 1;
		[Config] [AECategory("MipLevel3")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip3_LodBias1 { get; set; } = 1.5f;
		[Config] [AECategory("MipLevel3")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip3_LodBias2 { get; set; } = 3.5f;

		[Config] [AECategory("MipLevel4")] [AEValueRange(-12, 0, 1.0f, 0.1f)] public float Mip4_Weight1  { get; set; } = -1;
		[Config] [AECategory("MipLevel4")] [AEValueRange(-12, 0, 1.0f, 0.1f)] public float Mip4_Weight2  { get; set; } = -1;
		[Config] [AECategory("MipLevel4")] [AEValueRange(  0,10, 0.5f, 0.1f)] public float Mip4_Radius1  { get; set; } =  3;
		[Config] [AECategory("MipLevel4")] [AEValueRange(  0,10, 0.5f, 0.1f)] public float Mip4_Radius2  { get; set; } =  5;
		[Config] [AECategory("MipLevel4")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip4_LodBias0 { get; set; } = 2.5f;
		[Config] [AECategory("MipLevel4")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip4_LodBias1 { get; set; } = 2.5f;
		[Config] [AECategory("MipLevel4")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip4_LodBias2 { get; set; } = 2.5f;

		[Config] [AECategory("MipLevel5")] [AEValueRange(-12, 0, 1.0f, 0.1f)] public float Mip5_Weight1  { get; set; } = -1;
		[Config] [AECategory("MipLevel5")] [AEValueRange(-12, 0, 1.0f, 0.1f)] public float Mip5_Weight2  { get; set; } = -1;
		[Config] [AECategory("MipLevel5")] [AEValueRange(  0,10, 0.5f, 0.1f)] public float Mip5_Radius1  { get; set; } = 3;
		[Config] [AECategory("MipLevel5")] [AEValueRange(  0,10, 0.5f, 0.1f)] public float Mip5_Radius2  { get; set; } = 5;
		[Config] [AECategory("MipLevel5")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip5_LodBias0 { get; set; } = 1;
		[Config] [AECategory("MipLevel5")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip5_LodBias1 { get; set; } = 1;
		[Config] [AECategory("MipLevel5")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip5_LodBias2 { get; set; } = 1;

		[Config] [AECategory("MipLevel6")] [AEValueRange(-12, 0, 1.0f, 0.1f)] public float Mip6_Weight1  { get; set; } = -1;
		[Config] [AECategory("MipLevel6")] [AEValueRange(-12, 0, 1.0f, 0.1f)] public float Mip6_Weight2  { get; set; } =  0;
		[Config] [AECategory("MipLevel6")] [AEValueRange(  0,10, 0.5f, 0.1f)] public float Mip6_Radius1  { get; set; } =  0;
		[Config] [AECategory("MipLevel6")] [AEValueRange(  0,10, 0.5f, 0.1f)] public float Mip6_Radius2  { get; set; } = 10;
		[Config] [AECategory("MipLevel6")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip6_LodBias0 { get; set; } = 0;
		[Config] [AECategory("MipLevel6")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip6_LodBias1 { get; set; } = 0;
		[Config] [AECategory("MipLevel6")] [AEValueRange(  0, 5, 0.5f, 0.1f)] public float Mip6_LodBias2 { get; set; } = 0;


		ConstantBuffer	cbFilterData;
		Vector4[]		filterData;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		public CubeMapFilter( RenderSystem rs ) : base( rs )
		{
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Initialize()
		{
			cbFilterData	=	new ConstantBuffer( rs.Device, typeof(Vector4), 20 * 7 );
			filterData		=	new Vector4[ 20 * 7 ];

			LoadContent();
			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shader	=	Game.Content.Load<Ubershader>("cubegen");
			factory	=	shader.CreateFactory( typeof(Flags) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if (disposing) 
			{
				SafeDispose( ref cbFilterData );
			}

			base.Dispose( disposing );
		}


		void GenerateFilterData()
		{
			GenerateFilterDataForMip( 0, filterData, Mip0_Weight1, Mip0_Weight2, Mip0_Radius1, Mip0_Radius2, Mip0_LodBias0, Mip0_LodBias1, Mip0_LodBias2 );
			GenerateFilterDataForMip( 1, filterData, Mip1_Weight1, Mip1_Weight2, Mip1_Radius1, Mip1_Radius2, Mip1_LodBias0, Mip1_LodBias1, Mip1_LodBias2 );
			GenerateFilterDataForMip( 2, filterData, Mip2_Weight1, Mip2_Weight2, Mip2_Radius1, Mip2_Radius2, Mip2_LodBias0, Mip2_LodBias1, Mip2_LodBias2 );
			GenerateFilterDataForMip( 3, filterData, Mip3_Weight1, Mip3_Weight2, Mip3_Radius1, Mip3_Radius2, Mip3_LodBias0, Mip3_LodBias1, Mip3_LodBias2 );
			GenerateFilterDataForMip( 4, filterData, Mip4_Weight1, Mip4_Weight2, Mip4_Radius1, Mip4_Radius2, Mip4_LodBias0, Mip4_LodBias1, Mip4_LodBias2 );
			GenerateFilterDataForMip( 5, filterData, Mip5_Weight1, Mip5_Weight2, Mip5_Radius1, Mip5_Radius2, Mip5_LodBias0, Mip5_LodBias1, Mip5_LodBias2 );
			GenerateFilterDataForMip( 6, filterData, Mip6_Weight1, Mip6_Weight2, Mip6_Radius1, Mip6_Radius2, Mip6_LodBias0, Mip6_LodBias1, Mip6_LodBias2 );

			cbFilterData.SetData( filterData );
		}


		void GenerateFilterDataForMip( int mip, Vector4[] data, float w1, float w2, float r1, float r2, float b0, float b1, float b2 )
		{
			w1 = (float)Math.Pow( 2.0f, w1 );
			w2 = (float)Math.Pow( 2.0f, w2 );

			float w0	=	1 - w1 - w2;

			data[ mip * 20 ] = new Vector4( 0, 0, b0, w0 );

			int size = RenderSystem.LightProbeSize >> mip;
			float dxy  =  1.0f / size;

			for (int i=0; i<6; i++)
			{
				float a = MathUtil.Pi * 2.0f * i / 6.0f;
				float x = (float)Math.Cos( a ) * r1 * dxy;
				float y = (float)Math.Sin( a ) * r1 * dxy;
				data[ mip * 20 + 1 + i ] = new Vector4( x, y, b1, w1 ); 
			}

			for (int i=0; i<12; i++)
			{
				float a = MathUtil.Pi * 2.0f * i / 12.0f;
				float x = (float)Math.Cos( a ) * r2 * dxy;
				float y = (float)Math.Sin( a ) * r2 * dxy;
				data[ mip * 20 + 1 + 6 + i ] = new Vector4( x, y, b2, w2 ); 
			}
		}

		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Light probe relighting :
		 * 
		-----------------------------------------------------------------------------------------*/

		public void GenerateCubeMipLevel( RenderTargetCube cubemap )
		{
			device.ResetStates();

			if (cubemap==null) throw new ArgumentNullException("cubemap");

			using ( new PixEvent( "GenerateCubeMipLevel" ) )
			{
				for ( int mip = 0; mip<cubemap.MipCount-1; mip++ )
				{
					device.PipelineState = factory[(int)Flags.DOWNSAMPLE];

					var source	=	cubemap.GetCubeShaderResource(mip);
					var target	=	cubemap.GetCubeSurface( mip + 1 ).UnorderedAccess;
					var size	=	cubemap.Width >> ( mip + 1 );

					device.SetComputeUnorderedAccess( regTarget, target );

					device.ComputeSamplers	[ regLinearSampler	]	=	SamplerState.LinearWrap;
					device.ComputeResources	[ regSource			]	=	source;


					int tgx		=	MathUtil.IntDivRoundUp( size, 8 );
					int tgy		=	MathUtil.IntDivRoundUp( size, 8 );
					int tgz		=	6; // for each face

					device.Dispatch( tgx, tgy, tgz );
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="lightSet"></param>
		/// <param name="target"></param>
		public void PrefilterLightProbe ( RenderTargetCube source, TextureCubeArrayRW targetArray, int targetIndex )
		{
			if (targetIndex<0) return;

			using ( new PixEvent( "PrefilterLightProbe" ) )
			{

				if ( source      == null ) throw new ArgumentNullException( "source" );
				if ( targetArray == null ) throw new ArgumentNullException( "targetArray" );

				Flags flag = Flags.PREFILTER;

				if ( UseReference )
				{
					flag |= Flags.REFERENCE;

					if ( UseDifference )
					{
						flag |= Flags.DIFFERENCE;
					}
				}

				device.PipelineState = factory[(int)flag];

				GenerateFilterData();

				var target0 =   targetArray.GetSingleCubeSurface( targetIndex, 0 );
				var target1 =   targetArray.GetSingleCubeSurface( targetIndex, 1 );
				var target2 =   targetArray.GetSingleCubeSurface( targetIndex, 2 );
				var target3 =   targetArray.GetSingleCubeSurface( targetIndex, 3 );
				var target4 =   targetArray.GetSingleCubeSurface( targetIndex, 4 );
				var target5 =   targetArray.GetSingleCubeSurface( targetIndex, 5 );
				var target6 =   targetArray.GetSingleCubeSurface( targetIndex, 6 );

				device.SetComputeUnorderedAccess( 0, target0.UnorderedAccess );
				device.SetComputeUnorderedAccess( 1, target1.UnorderedAccess );
				device.SetComputeUnorderedAccess( 2, target2.UnorderedAccess );
				device.SetComputeUnorderedAccess( 3, target3.UnorderedAccess );
				device.SetComputeUnorderedAccess( 4, target4.UnorderedAccess );
				device.SetComputeUnorderedAccess( 5, target5.UnorderedAccess );
				device.SetComputeUnorderedAccess( 6, target6.UnorderedAccess );

				device.ComputeSamplers[regLinearSampler]   =   SamplerState.LinearWrap;
				device.ComputeResources[regSource]   =   source;
				device.ComputeConstants[regFilterData]   =   cbFilterData;

				int size    =   128*128 + 64*64 + 32*32 + 16*16 + 8*8 + 4*4 + 2*2;
				int tgx     =   MathUtil.IntDivRoundUp( size, 64 );
				int tgy     =   6; // num faces
				int tgz     =   1;

				device.Dispatch( tgx, tgy, tgz );
			}
		}
	}
}
