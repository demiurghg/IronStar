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
using SharpDX.Mathematics.Interop;


namespace Fusion.Drivers.Graphics {
	public class Texture3DCompute : ShaderResource {

		D3D.Texture3D	tex3D;

		UnorderedAccessView uav;

		internal UnorderedAccessView Uav {
			get {
				return uav;
			}
		}

		UnorderedAccess unorderedAccess;
		internal UnorderedAccess UnorderedAccess {
			get {
				return unorderedAccess; 
			}
		}

		/// <summary>
		/// Creates texture
		/// </summary>
		/// <param name="device"></param>
		public Texture3DCompute ( GraphicsDevice device, ColorFormat format, int width, int height, int depth ) : base( device )
		{
			this.Width		=	width;
			this.Height		=	height;
			this.Depth		=	depth;

			var texDesc = new Texture3DDescription();
			texDesc.BindFlags		=	BindFlags.ShaderResource|BindFlags.UnorderedAccess;
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
			uav		=	new D3D.UnorderedAccessView( device.Device, tex3D );

			unorderedAccess	=	new UnorderedAccess( device, uav );
		}



		/// <summary>
		/// 
		/// </summary>
		public void Clear ( Vector4 clearValue )
		{
			var rawClearValue =	new RawVector4( clearValue.X, clearValue.Y, clearValue.Z, clearValue.W );
			device.DeviceContext.ClearUnorderedAccessView( uav, rawClearValue );
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
				SafeDispose( ref uav );
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
			throw new NotImplementedException();
			//device.DeviceContext.UpdateSubresource(data, tex3D, 0, Width*8, Height*Width*8);
		}
	}
}
