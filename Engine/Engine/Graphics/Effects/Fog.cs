using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics.Ubershaders;

namespace Fusion.Engine.Graphics {

	[RequireShader("fog", true)]
	internal class Fog : RenderComponent {

		const int FogSizeX		=	64;
		const int FogSizeY		=	32;
		const int FogSizeZ		=	92;

		[ShaderDefine]
		const int BlockSizeX	=	4;

		[ShaderDefine]
		const int BlockSizeY	=	4;

		[ShaderDefine]
		const int BlockSizeZ	=	4;




		[Flags]
		enum FogFlags : int
		{
			COMPUTE		= 0x0001,
			INTEGRATE	= 0x0002,
		}

		//	row_major float4x4 MatrixWVP;      // Offset:    0 Size:    64 [unused]
		//	float3 SunPosition;                // Offset:   64 Size:    12
		//	float4 SunColor;                   // Offset:   80 Size:    16
		//	float Turbidity;                   // Offset:   96 Size:     4 [unused]
		//	float3 Temperature;                // Offset:  100 Size:    12
		//	float SkyIntensity;                // Offset:  112 Size:     4
		[ShaderStructure]
		[StructLayout(LayoutKind.Explicit, Size=160)]
		struct FogConsts {
			[FieldOffset(  0)] public Matrix 	MatrixWVP;
			[FieldOffset( 64)] public Vector3	SunPosition;
			[FieldOffset( 80)] public Color4	SunColor;
			[FieldOffset( 96)] public float		Turbidity;
			[FieldOffset(100)] public Vector3	Temperature; 
			[FieldOffset(112)] public float		SkyIntensity; 
			[FieldOffset(116)] public Vector3	Ambient;
			[FieldOffset(128)] public float		Time;
			[FieldOffset(132)] public Vector3	ViewPos;
			[FieldOffset(136)] public float		SunAngularSize;

		}


		Ubershader			shader;
		StateFactory		factory;

		Texture3DCompute	fog3d0;
		Texture3DCompute	fog3d1;



		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rs"></param>
		public Fog ( RenderSystem rs ) : base( rs )
		{
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Initialize() 
		{
			fog3d0	=	new Texture3DCompute( device, FogSizeX, FogSizeY, FogSizeZ );
			fog3d1	=	new Texture3DCompute( device, FogSizeX, FogSizeY, FogSizeZ );

			LoadContent();

			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shader		=	Game.Content.Load<Ubershader>("fog");
			factory		=	shader.CreateFactory( typeof(FogFlags) );
		}



		/// <summary>
		/// 
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing ) {
				SafeDispose( ref fog3d0 );
				SafeDispose( ref fog3d1 );
			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// Renders fog look-up table
		/// </summary>
		internal void RenderFog( Camera camera, FogSettings settings )
		{
			using ( new PixEvent("Fog") ) {
				
				device.ResetStates();

				device.PipelineState	=	factory[ (int)FogFlags.COMPUTE ];

				device.ComputeShaderResources[0]	=	fog3d0;

				device.SetCSRWTexture( 0, fog3d1 );

				var gx	=	MathUtil.IntDivUp( FogSizeX, BlockSizeX );
				var gy	=	MathUtil.IntDivUp( FogSizeY, BlockSizeY );
				var gz	=	MathUtil.IntDivUp( FogSizeZ, BlockSizeZ );

				device.Dispatch( gx, gy, gz );
			}
		}
	}
}
