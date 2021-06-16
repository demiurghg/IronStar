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
using Fusion.Core.Extensions;
using SharpDX.Direct3D;
using Native.Dds;
using Fusion.Core;
using Fusion.Engine.Common;


namespace Fusion.Drivers.Graphics 
{
	internal class Texture3D : ShaderResource 
	{
		D3D.Texture3D	tex3D;

		ColorFormat format;


		/// <summary>
		/// Creates texture
		/// </summary>
		/// <param name="device"></param>
		public Texture3D ( GraphicsDevice device, ColorFormat format, int width, int height, int depth ) : base( device )
		{
			this.Width		=	width;
			this.Height		=	height;
			this.Depth		=	depth;
			this.format		=	format;

			var texDesc = new Texture3DDescription();
			texDesc.BindFlags		=	BindFlags.ShaderResource;
			texDesc.CpuAccessFlags	=	CpuAccessFlags.None;
			texDesc.Format			=	Converter.Convert( format );
			texDesc.Height			=	Height;
			texDesc.MipLevels		=	1;
			texDesc.OptionFlags		=	ResourceOptionFlags.None;
			texDesc.Usage			=	ResourceUsage.Default;
			texDesc.Width			=	Width;
			texDesc.Depth			=	Depth;

			tex3D	=	new D3D.Texture3D( device.Device, texDesc );
			SRV		=	new D3D.ShaderResourceView( device.Device, tex3D );
		}



		/// <summary>
		/// Disposes
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref tex3D );
				SafeDispose( ref SRV );
			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// Sets 3D texture data.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="data"></param>
        public void SetData<T>(T[] data) where T: struct
		{
			var elementSizeInByte = Marshal.SizeOf(typeof(T));
			var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

			lock (device.DeviceContext) 
			{
				try
				{
					var dataPtr		=	(IntPtr)(dataHandle.AddrOfPinnedObject().ToInt64());

					int rowPitch	=	Converter.SizeOf(this.format) * Width;
					int slicePitch	=	rowPitch * Height; // For 3D texture: Size of 2D image.
					var box			=	new DataBox(dataPtr, rowPitch, slicePitch);

					int subresourceIndex = 0;

					var region		=	new ResourceRegion(0, 0, 0, Width, Height, Depth);

					device.DeviceContext.UpdateSubresource(box, tex3D, subresourceIndex, region);
				}
				finally
				{
					dataHandle.Free();
				}
			}
		}
	}
}
