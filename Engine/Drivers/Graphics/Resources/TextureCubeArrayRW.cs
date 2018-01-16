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

		RenderTargetSurface[,]	cubeSurfaces;
		ShaderResource[,]		cubeResources;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="device"></param>
		/// <param name="size"></param>
		/// <param name="count"></param>
		/// <param name="format"></param>
		/// <param name="mips"></param>
		public TextureCubeArrayRW ( GraphicsDevice device, int size, int count, ColorFormat format, bool mips ) : base(device)
		{
			if (count>2048/6) {
				throw new GraphicsException("Too much elements in texture array");
			}

			this.Width		=	size;
			this.Depth		=	1;
			this.Height		=	size;
			this.MipCount	=	mips ? ShaderResource.CalculateMipLevels(Width,Height) : 1;

			var texDesc = new Texture2DDescription();

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

			cubeSurfaces	=	new RenderTargetSurface	[count, MipCount];
			cubeResources	=	new ShaderResource		[count, MipCount];

			
			for ( int index=0; index<count; index++ ) {
				
				for ( int mip=0; mip<MipCount; mip++ ) {

					int mipSize = Math.Max(1, size >> mip);

					var srvDesc = new ShaderResourceViewDescription();
						srvDesc.TextureCubeArray.MipLevels			=	1;
						srvDesc.TextureCubeArray.CubeCount			=	1;
						srvDesc.TextureCubeArray.First2DArrayFace	=	index * 6;
						srvDesc.TextureCubeArray.MostDetailedMip	=	mip;
						srvDesc.Format		=	Converter.Convert( format );
						srvDesc.Dimension	=	ShaderResourceViewDimension.TextureCubeArray;

					var srv	=	new ShaderResourceView( device.Device, texCubeArray, srvDesc );

					cubeResources[index,mip]	=	new ShaderResource( device, srv, mipSize, mipSize, 1 );


					var uavDesc = new UnorderedAccessViewDescription();
						uavDesc.Dimension			=	UnorderedAccessViewDimension.Texture2DArray;
						uavDesc.Format				=	Converter.Convert( format );
						uavDesc.Texture2DArray.ArraySize		=	6;
						uavDesc.Texture2DArray.FirstArraySlice	=	index * 6;
						uavDesc.Texture2DArray.MipSlice			=	mip;

					var uav	=	new UnorderedAccessView( device.Device, texCubeArray, uavDesc );

					cubeSurfaces[index,mip]	=	new RenderTargetSurface( null, uav, texCubeArray, -1, format, mipSize, mipSize, 1 );

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
				SafeDispose( ref cubeSurfaces );
				SafeDispose( ref cubeResources );
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
		public RenderTargetSurface GetCubeSurface ( int index, int mip )
		{							
			return cubeSurfaces[ index, mip ];
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="mip"></param>
		/// <returns></returns>
		public ShaderResource GetCubeShaderResource ( int index, int mip )
		{							
			return cubeResources[ index, mip ];
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

				int subResourceCount = 6 * rtCube.MipCount;
			
				for (int i=0; i<subResourceCount; i++) {
				
					int srcIndex = i;
					int dstIndex = i + subResourceCount * index;

					GraphicsDevice.DeviceContext.CopySubresourceRegion( rtCube.TextureResource, srcIndex, null, texCubeArray, dstIndex );
				}
			}
		}

	}
}
