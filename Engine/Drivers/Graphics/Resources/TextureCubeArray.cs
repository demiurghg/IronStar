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
	internal class TextureCubeArray : ShaderResource {

		internal readonly D3D.Texture2D	texCubeArray;

		public readonly int MipCount;

		public readonly ColorFormat Format;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="device"></param>
		/// <param name="size"></param>
		/// <param name="count"></param>
		/// <param name="format"></param>
		/// <param name="mips"></param>
		public TextureCubeArray ( GraphicsDevice device, int size, int count, ColorFormat format, int mipCount ) : base(device)
		{
			if (count>2048/6) {
				throw new GraphicsException("Too much elements in texture array");
			}

			this.Width		=	size;
			this.Depth		=	1;
			this.Height		=	size;
			this.MipCount	=	mipCount==0 ? CalculateMipLevels(Width,Height) : mipCount;
			this.Format		=	format;

			var texDesc = new Texture2DDescription();

			texDesc.ArraySize		=	6 * count;
			texDesc.BindFlags		=	BindFlags.ShaderResource;
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
		}


		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				if (texCubeArray!=null) {
					texCubeArray.Dispose();
				}
			}
			base.Dispose( disposing );
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


		/// <summary>
		/// Sets 2D texture data, specifying a mipmap level, source rectangle, start index, and number of elements.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="level"></param>
		/// <param name="data"></param>
		/// <param name="startIndex"></param>
		/// <param name="elementCount"></param>
		public void SetData<T>( int cubeIndex, CubeFace face, int level, T[] data ) where T: struct
		{
			var elementSizeInByte	=	Marshal.SizeOf(typeof(T));
			var dataHandle			=	GCHandle.Alloc(data, GCHandleType.Pinned);

			try {
				var dataPtr		=	dataHandle.AddrOfPinnedObject();

				int x = 0;
				int y = 0;
				int w = Math.Max(Width >> level, 1);
				int h = Math.Max(Height >> level, 1);

				int subres	=	Resource.CalculateSubResourceIndex( level, cubeIndex * 6 + (int)face, MipCount );

				var box = new SharpDX.DataBox(dataPtr, w * Converter.SizeOf(Format), 0);

				var region		= new SharpDX.Direct3D11.ResourceRegion();
				region.Top		= y;
				region.Front	= 0;
				region.Back		= 1;
				region.Bottom	= y + h;
				region.Left		= x;
				region.Right	= x + w;

				lock (device.DeviceContext) {
					device.DeviceContext.UpdateSubresource(box, texCubeArray, subres, region);
				}

			} finally {
				dataHandle.Free();
			}
		}
	}
}
