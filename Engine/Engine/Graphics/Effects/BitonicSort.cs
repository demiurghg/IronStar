﻿using System;
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
	[RequireShader("bitonicSort")]
	internal class BitonicSort : RenderComponent {

		struct Params {
			public uint Level;
			public uint LevelMask;
			public uint Width;
			public uint Height;
		}


		enum ShaderFlags {
			BITONIC_SORT = 1,
			TRANSPOSE = 2,
		}

		const int NumberOfElements		=	256*256;
		const int BitonicBlockSize		=	256;
		const int TransposeBlockSize	=	16;
		const int MatrixWidth			=	BitonicBlockSize;
		const int MatrixHeight			=	NumberOfElements / BitonicBlockSize;

		ConstantBuffer		paramsCB;
		StructuredBuffer	buffer2;
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
			buffer2		=	new StructuredBuffer( device, typeof(Vector2), NumberOfElements  , StructuredBufferFlags.None );
			paramsCB	=	new ConstantBuffer( device, typeof(Params) );

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
				SafeDispose( ref buffer2	);
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
			Params p = new Params(){ Level = iLevel, LevelMask = iLevelMask, Width = iWidth, Height = iHeight };

			paramsCB.SetData( p );
			device.ComputeShaderConstants[0]	= paramsCB ;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		public void Sort ( StructuredBuffer buffer )
		{
			device.ResetStates();

			var outputBytes		= new byte[NumberOfElements*4*2];
			var outputVectors	= new Vector2[NumberOfElements];
			buffer.GetData( outputBytes );
			buffer.GetData( outputVectors );

			var inputData = File.ReadAllBytes(@"D:\Github\bitonicSort.dat");
			buffer.SetData(inputData);

			device.Clear( buffer2, Int4.Zero );

			using (new PixEvent("Pass#1")) {

				//	First sort the rows for the levels <= to the block size
				for( uint level=2; level<=BitonicBlockSize; level = level * 2 ) {

					SetConstants( level, level, MatrixWidth, MatrixHeight );

					// Sort the row data
					device.SetCSRWBuffer( 0, buffer );
					device.PipelineState	=	factory[ (int)ShaderFlags.BITONIC_SORT ];
					device.Dispatch( NumberOfElements / BitonicBlockSize, 1, 1 );
				}
			}

			using (new PixEvent("Pass#2")) {

				for( uint level = (BitonicBlockSize * 2); level <= NumberOfElements; level = level * 2 ){

					PixEvent.Marker(string.Format("Level = {0}", level));

					SetConstants( (level / BitonicBlockSize), (uint)(level & ~NumberOfElements) / BitonicBlockSize, MatrixWidth, MatrixHeight );

					// Transpose the data from buffer 1 into buffer 2
					device.ComputeShaderResources[0]	=	null;
					device.SetCSRWBuffer( 0, buffer2 );
					device.ComputeShaderResources[0]	=	buffer;
					device.PipelineState				=	factory[ (int)ShaderFlags.TRANSPOSE ];
					device.Dispatch( MatrixWidth / TransposeBlockSize, MatrixHeight / TransposeBlockSize, 1 );

					// Sort the transposed column data
					device.PipelineState	=	factory[ (int)ShaderFlags.BITONIC_SORT ];
					device.Dispatch( NumberOfElements / BitonicBlockSize, 1, 1 );


					SetConstants( BitonicBlockSize, level, MatrixWidth, MatrixHeight );

					// Transpose the data from buffer 2 back into buffer 1
					device.ComputeShaderResources[0]	=	null;
					device.SetCSRWBuffer( 0, buffer );
					device.ComputeShaderResources[0]	=	buffer2;
					device.PipelineState				=	factory[ (int)ShaderFlags.TRANSPOSE ];
					device.Dispatch( MatrixHeight / TransposeBlockSize, MatrixHeight / TransposeBlockSize, 1 );

					// Sort the row data
					device.PipelineState	=	factory[ (int)ShaderFlags.BITONIC_SORT ];
					device.Dispatch( NumberOfElements / BitonicBlockSize, 1, 1 );
				}
			}


			//
			//	Check results 
			//
			#if true
			//if (Game.Keyboard.IsKeyDown(Keys.K)) {

				int errorCount = 0;

				//Log.Message("-- Bitonic sort check --");

				var output = new Vector2[NumberOfElements];

				buffer.GetData( output );
	
				for (int i=0; i<NumberOfElements; i++) {
					
					bool error = (i < NumberOfElements-1) ? output[i].X>output[i+1].X : false;

					if (error) {
						errorCount ++;
					}
					if (error) {
						//Log.Message("{0,4} : {1,6:0.00} - {2,6:0.00} {3}", i, output[i].X, output[i].Y, error?"<- Error":"" );
					}
				}

				if (errorCount>0) {
					Log.Warning("Sort errors : {0}", errorCount );
				}

				if (errorCount>100 && !File.Exists(@"D:\Github\bitonicSort.dat")) {

					File.WriteAllBytes(@"D:\Github\bitonicSort.dat", outputBytes );

					StringBuilder sb = new StringBuilder();

					for (int i=0; i<NumberOfElements; i++) {
						sb.AppendFormat("{0}\t{1}\t{2}\r\n", i, outputVectors[i].X, outputVectors[i].Y);
					}
					File.WriteAllText(@"D:\Github\bitonicSortA.txt", sb.ToString() );

					for (int i=0; i<NumberOfElements; i++) {
						sb.AppendFormat("{0}\t{1}\t{2}\r\n", i, output[i].X, output[i].Y);
					}
					File.WriteAllText(@"D:\Github\bitonicSortB.txt", sb.ToString() );

					Log.Warning("Sort errors : {0}", errorCount );
				}
			//}
			#endif
		}
	}
}
