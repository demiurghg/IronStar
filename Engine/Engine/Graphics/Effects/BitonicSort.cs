using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using Fusion.Engine.Graphics.Ubershaders;
using System.IO;


namespace Fusion.Engine.Graphics
{
	/// <summary>
	/// Class for base image processing such as copying, blurring, enhancement, anti-aliasing etc.
	/// </summary>
	[RequireShader("bitonicSort", true)]
	internal class BitonicSort : RenderComponent {

		[ShaderStructure]
		struct SORT_DATA {
			public uint Level;
			public uint LevelMask;
			public uint Width;
			public uint Height;
		}


		enum ShaderFlags {
			BITONIC_SORT = 1,
			TRANSPOSE = 2,
		}

		[ShaderDefine]		const int NumberOfElements		=	256*256;
		[ShaderDefine]		const int BitonicBlockSize		=	256;
		[ShaderDefine]		const int TransposeBlockSize	=	16;
		[ShaderDefine]		const int MatrixWidth			=	BitonicBlockSize;
		[ShaderDefine]		const int MatrixHeight			=	NumberOfElements / BitonicBlockSize;

		static FXConstantBuffer<SORT_DATA>	regSortData		=	new CRegister( 0, "SortData" );

		ConstantBuffer		paramsCB;
		StructuredBuffer	tempBuffer;

		Ubershader			shader;
		StateFactory		factory;

		
		public BitonicSort( RenderSystem rs ) : base( rs )
		{
		}



		/// <summary>
		/// Initializes Filter service
		/// </summary>
		public override void Initialize() 
		{
			//	create structured buffers and shaders :
			tempBuffer	=	new StructuredBuffer( device, typeof(Vector2), NumberOfElements  , StructuredBufferFlags.None );
			paramsCB	=	new ConstantBuffer( device, typeof(SORT_DATA) );

			LoadContent();
			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shader		=	Game.Content.Load<Ubershader>("bitonicSort");
			factory		=	shader.CreateFactory( typeof(ShaderFlags), Primitive.TriangleList, VertexInputElement.Empty );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				SafeDispose( ref tempBuffer	);
				SafeDispose( ref paramsCB	);
			}

			base.Dispose( disposing );
		}





		/// <summary>
		/// 
		/// </summary>
		/// <param name="iLevel"></param>
		/// <param name="iLevelMask"></param>
		/// <param name="iWidth"></param>
		/// <param name="iHeight"></param>
		void SetConstants( uint iLevel, uint iLevelMask, uint iWidth, uint iHeight )
		{
			SORT_DATA p = new SORT_DATA(){ Level = iLevel, LevelMask = iLevelMask, Width = iWidth, Height = iHeight };

			paramsCB.SetData( ref p );
			device.ComputeConstants[0]	= paramsCB ;
		}



		public void CheckResults (StructuredBuffer buffer)
		{
			var output = new Vector2[NumberOfElements];

			int nan = 0;
			int pinf = 0;
			int ninf = 0;
			int errors = 0;

			buffer.GetData( output );
	
			for (int i=0; i<NumberOfElements; i++) {
					
				bool error = (i < NumberOfElements-1) ? output[i].X>output[i+1].X : false;

				if ( float.IsNaN( output[i].X ) ) nan++;
				if ( float.IsPositiveInfinity( output[i].X ) ) pinf++;
				if ( float.IsNegativeInfinity( output[i].X ) ) ninf++;

				if (error) {
					errors ++;
				}
			}

			if (nan>0 || pinf>0 || ninf>0) {
				Log.Warning("BitonicSort: errors:{0} nan:{1} pInf:{2} nInf{3}", errors, nan, pinf, ninf );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		public void Sort ( StructuredBuffer buffer )
		{
			device.ResetStates();

			using (new PixEvent("Pass#1")) {

				//	First sort the rows for the levels <= to the block size
				for( uint level=2; level<=BitonicBlockSize; level = level * 2 ) {

					SetConstants( level, level, MatrixWidth, MatrixHeight );

					// Sort the row data
					device.SetComputeUnorderedAccess( 0, buffer.UnorderedAccess );
					device.PipelineState	=	factory[ (int)ShaderFlags.BITONIC_SORT ];
					device.Dispatch( NumberOfElements / BitonicBlockSize, 1, 1 );
				}
			}

			using (new PixEvent("Pass#2")) {

				for( uint level = (BitonicBlockSize * 2); level <= NumberOfElements; level = level * 2 ){

					PixEvent.Marker(string.Format("Level = {0}", level));

					SetConstants( (level / BitonicBlockSize), (uint)(level & ~NumberOfElements) / BitonicBlockSize, MatrixWidth, MatrixHeight );

					// Transpose the data from buffer 1 into buffer 2
					device.ComputeResources[0]	=	null;
					device.SetComputeUnorderedAccess( 0, tempBuffer.UnorderedAccess );
					device.ComputeResources[0]	=	buffer;
					device.PipelineState				=	factory[ (int)ShaderFlags.TRANSPOSE ];
					device.Dispatch( MatrixWidth / TransposeBlockSize, MatrixHeight / TransposeBlockSize, 1 );

					// Sort the transposed column data
					device.PipelineState	=	factory[ (int)ShaderFlags.BITONIC_SORT ];
					device.Dispatch( NumberOfElements / BitonicBlockSize, 1, 1 );


					SetConstants( BitonicBlockSize, level, MatrixWidth, MatrixHeight );

					// Transpose the data from buffer 2 back into buffer 1
					device.ComputeResources[0]	=	null;
					device.SetComputeUnorderedAccess( 0, buffer.UnorderedAccess );
					device.ComputeResources[0]	=	tempBuffer;
					device.PipelineState				=	factory[ (int)ShaderFlags.TRANSPOSE ];
					device.Dispatch( MatrixHeight / TransposeBlockSize, MatrixHeight / TransposeBlockSize, 1 );

					// Sort the row data
					device.PipelineState	=	factory[ (int)ShaderFlags.BITONIC_SORT ];
					device.Dispatch( NumberOfElements / BitonicBlockSize, 1, 1 );
				}
			}

			#if false
			CheckResults( buffer );
			#endif
		}
	}
}
