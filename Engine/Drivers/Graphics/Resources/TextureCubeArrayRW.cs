using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DXGI = SharpDX.DXGI;
using D3D = SharpDX.Direct3D11;
using SharpDX;
using SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using SharpDX.Direct3D;
using Native.Dds;
using Native.Wic;
using Fusion.Core;
using Fusion.Engine.Common;


namespace Fusion.Drivers.Graphics {
	internal class TextureCubeArrayRW : ShaderResource {

		internal readonly D3D.Texture2D	texCubeArray;

		public readonly int MipCount;

		RenderTargetSurface[,]	singleCubeSurfaces;
		RenderTargetSurface[,]	batchCubeSurfaces;
		ShaderResource[,]		batchCubeResources;
		D3D.Texture2D			stagingCube;
		readonly int batchSize;
		readonly int batchCount;
		readonly ColorFormat format;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="device"></param>
		/// <param name="size"></param>
		/// <param name="count"></param>
		/// <param name="format"></param>
		/// <param name="mips"></param>
		public TextureCubeArrayRW ( GraphicsDevice device, int size, int count, ColorFormat format, bool mips, int batchSize=1 ) : base(device)
		{
			if (count>2048/6) {
				throw new GraphicsException("Too much elements in texture array");
			}

			if ((count/batchSize)*batchSize!=count) {
				throw new ArgumentException("Argument 'batchSize' must be multiple of 'count'");
			}

			this.batchSize	=	batchSize;
			this.batchCount	=	count / batchSize;
			this.Width		=	size;
			this.Depth		=	1;
			this.Height		=	size;
			this.MipCount	=	mips ? ShaderResource.CalculateMipLevels(Width,Height) : 1;
			this.format		=	format;

			var texDesc = new Texture2DDescription();

			//-------------------------------------------
			//	create staging cube texture :
			//-------------------------------------------

			texDesc.ArraySize		=	6;
			texDesc.BindFlags		=	BindFlags.None;
			texDesc.CpuAccessFlags	=	CpuAccessFlags.Read|CpuAccessFlags.Write;
			texDesc.Format			=	Converter.Convert( format );
			texDesc.Height			=	Height;
			texDesc.MipLevels		=	MipCount;
			texDesc.OptionFlags		=	ResourceOptionFlags.TextureCube;
			texDesc.SampleDescription.Count	=	1;
			texDesc.SampleDescription.Quality	=	0;
			texDesc.Usage			=	ResourceUsage.Staging;
			texDesc.Width			=	Width;

			stagingCube				=	new D3D.Texture2D( device.Device, texDesc );

			//-------------------------------------------
			//	create cube texture array :
			//-------------------------------------------

			texDesc.ArraySize		=	6 * count;
			texDesc.BindFlags		=	BindFlags.ShaderResource|BindFlags.UnorderedAccess;
			texDesc.CpuAccessFlags	=	CpuAccessFlags.None;
			texDesc.Format			=	Converter.Convert( format );
			texDesc.Height			=	Height;
			texDesc.MipLevels		=	MipCount;
			texDesc.OptionFlags		=	ResourceOptionFlags.TextureCube;
			texDesc.SampleDescription.Count	=	1;
			texDesc.SampleDescription.Quality	=	0;
			texDesc.Usage			=	ResourceUsage.Default;
			texDesc.Width			=	Width;

			texCubeArray	=	new D3D.Texture2D( device.Device, texDesc );
			SRV				=	new ShaderResourceView( device.Device, texCubeArray );

			singleCubeSurfaces	=	new RenderTargetSurface	[count,		 MipCount];
			batchCubeSurfaces	=	new RenderTargetSurface	[batchCount, MipCount];
			batchCubeResources	=	new ShaderResource		[batchCount, MipCount];

			for ( int batchIndex=0; batchIndex<batchCount; batchIndex++ ) {
				
				for ( int mip=0; mip<MipCount; mip++ ) {

					int mipSize = Math.Max(1, size >> mip);

					var srvDesc = new ShaderResourceViewDescription();
						srvDesc.TextureCubeArray.MipLevels			=	1;
						srvDesc.TextureCubeArray.CubeCount			=	batchSize;
						srvDesc.TextureCubeArray.First2DArrayFace	=	batchIndex * 6 * batchSize;
						srvDesc.TextureCubeArray.MostDetailedMip	=	mip;
						srvDesc.Format		=	Converter.Convert( format );
						srvDesc.Dimension	=	ShaderResourceViewDimension.TextureCubeArray;

					var srv	=	new ShaderResourceView( device.Device, texCubeArray, srvDesc );

					batchCubeResources[batchIndex,mip]	=	new ShaderResource( device, srv, mipSize, mipSize, 1 );


					var uavDesc = new UnorderedAccessViewDescription();
						uavDesc.Dimension			=	UnorderedAccessViewDimension.Texture2DArray;
						uavDesc.Format				=	Converter.Convert( format );
						uavDesc.Texture2DArray.ArraySize		=	6 * batchSize;
						uavDesc.Texture2DArray.FirstArraySlice	=	batchIndex * 6 * batchSize;
						uavDesc.Texture2DArray.MipSlice			=	mip;

					var uav	=	new UnorderedAccessView( device.Device, texCubeArray, uavDesc );

					batchCubeSurfaces[batchIndex,mip]	=	new RenderTargetSurface( device, null, uav, texCubeArray, -1, format, mipSize, mipSize, 1 );

				}

			}


			for ( int index=0; index<count; index++ ) {
				
				for ( int mip=0; mip<MipCount; mip++ ) {

					int mipSize = Math.Max(1, size >> mip);

					//var srvDesc = new ShaderResourceViewDescription();
					//	srvDesc.TextureCubeArray.MipLevels			=	1;
					//	srvDesc.TextureCubeArray.CubeCount			=	batchSize;
					//	srvDesc.TextureCubeArray.First2DArrayFace	=	index * 6;
					//	srvDesc.TextureCubeArray.MostDetailedMip	=	mip;
					//	srvDesc.Format		=	Converter.Convert( format );
					//	srvDesc.Dimension	=	ShaderResourceViewDimension.TextureCubeArray;

					//var srv	=	new ShaderResourceView( device.Device, texCubeArray, srvDesc );

					//batchCubeResources[batchIndex,mip]	=	new ShaderResource( device, srv, mipSize, mipSize, 1 );

					var uavDesc = new UnorderedAccessViewDescription();
						uavDesc.Dimension			=	UnorderedAccessViewDimension.Texture2DArray;
						uavDesc.Format				=	Converter.Convert( format );
						uavDesc.Texture2DArray.ArraySize		=	6;
						uavDesc.Texture2DArray.FirstArraySlice	=	index * 6;
						uavDesc.Texture2DArray.MipSlice			=	mip;

					var uav	=	new UnorderedAccessView( device.Device, texCubeArray, uavDesc );

					singleCubeSurfaces[index,mip]	=	new RenderTargetSurface( device, null, uav, texCubeArray, -1, format, mipSize, mipSize, 1 );

				}

			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref singleCubeSurfaces );

				SafeDispose( ref batchCubeSurfaces );
				SafeDispose( ref batchCubeResources );
				texCubeArray?.Dispose();
			}

			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="mip"></param>
		/// <returns></returns>
		public RenderTargetSurface GetSingleCubeSurface ( int index, int mip )
		{							
			return singleCubeSurfaces[ index, mip ];
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="mip"></param>
		/// <returns></returns>
		public RenderTargetSurface GetBatchCubeSurface ( int batchIndex, int mip )
		{							
			return batchCubeSurfaces[ batchIndex, mip ];
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="mip"></param>
		/// <returns></returns>
		public ShaderResource GetBatchCubeShaderResource ( int batchIndex, int mip )
		{							
			return batchCubeResources[ batchIndex, mip ];
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="rtSourceCube"></param>
		public void CopyTopMipLevelFromRenderTargetCube ( int index, RenderTargetCube rtSourceCube )
		{
			using ( new PixEvent( "CopyTopMipLevelFromRenderTargetCube" ) ) {

				
				if (rtSourceCube.Width!=this.Width) {
					throw new GraphicsException("CopyFromRenderTargetCube: source and destination have different width");
				}

				if (rtSourceCube.Height!=this.Height) {
					throw new GraphicsException("CopyFromRenderTargetCube: source and destination have different height");
				}

				if (rtSourceCube.Format!=this.format) {
					throw new GraphicsException("CopyFromRenderTargetCube: source and destination have different format");
				}

				int subResourceCount = 6 * rtSourceCube.MipCount;
			
				for (int i=0; i<6; i++) {

					int srcIndex = Resource.CalculateSubResourceIndex( 0, i,			 rtSourceCube.MipCount );
					int dstIndex = Resource.CalculateSubResourceIndex( 0, index * 6 + i, this.MipCount );

					GraphicsDevice.DeviceContext.CopySubresourceRegion( rtSourceCube.TextureResource, srcIndex, null, texCubeArray, dstIndex );
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="rtCube"></param>
		public void CopyFromRenderTargetCube ( int index, RenderTargetCube rtCube )
		{
			using ( new PixEvent( "CopyFromRenderTargetCube" ) ) {

				if ( rtCube.MipCount!=this.MipCount ) {
					throw new GraphicsException( "CopyFromRenderTargetCube: source and destination have different mip count" );
				}

				if (rtCube.Width!=this.Width) {
					throw new GraphicsException("CopyFromRenderTargetCube: source and destination have different width");
				}

				if (rtCube.Height!=this.Height) {
					throw new GraphicsException("CopyFromRenderTargetCube: source and destination have different height");
				}

				if (rtCube.Format!=this.format) {
					throw new GraphicsException("CopyFromRenderTargetCube: source and destination have different format");
				}

				/*int subResourceCount = 6 * rtCube.MipCount;
			
				for (int i=0; i<subResourceCount; i++) {
				
					int srcIndex = i;
					int dstIndex = i + subResourceCount * index;				
				
					GraphicsDevice.DeviceContext.CopySubresourceRegion( rtCube.TextureResource, srcIndex, null, texCubeArray, dstIndex );
				} //*/
				int mipCount = rtCube.MipCount;
				int stride	 = 6;
				var context	 = GraphicsDevice.DeviceContext;

				for (int mip=0; mip<rtCube.MipCount; mip++) {
					for (int face=0; face<6; face++) {

						int srcIndex = Resource.CalculateSubResourceIndex( mip, face,                  mipCount );
						int dstIndex = Resource.CalculateSubResourceIndex( mip, face + stride * index, mipCount );;

						context.CopySubresourceRegion( rtCube.TextureResource, srcIndex, null, texCubeArray, dstIndex );
					}
				}
			}
		}



		void GetData<T> ( int mip, int index, CubeFace face, T[] data ) where T: struct
		{																	
			int faceId		=	(int)face;			
			int srcIndex	=	Resource.CalculateSubResourceIndex( mip, index * 6 + faceId, MipCount );
			int dstIndex	=	Resource.CalculateSubResourceIndex( mip,             faceId, MipCount );

			var context		=	GraphicsDevice.DeviceContext;

			context.CopySubresourceRegion( texCubeArray, srcIndex, null, stagingCube, dstIndex );

			DataStream stream;
			var dataBox = context.MapSubresource( stagingCube, dstIndex, MapMode.Read, MapFlags.None, out stream );

			int mipWidth		=	Width  >> mip;
			int mipHeight		=	Height >> mip;
            int currentIndex	=	0;
			int elementSize		=	Marshal.SizeOf(typeof(T));

			for ( int row=0; row<mipHeight; row++ ) {

                stream.ReadRange( data, currentIndex, mipWidth );
                stream.Seek( dataBox.RowPitch - (elementSize * mipWidth), SeekOrigin.Current);
                currentIndex += mipWidth;

			}

			stream.Dispose();

			context.UnmapSubresource( stagingCube, dstIndex );
		}
	}
}
