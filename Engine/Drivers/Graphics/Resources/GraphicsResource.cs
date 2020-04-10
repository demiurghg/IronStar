using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using D3D = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace Fusion.Drivers.Graphics
{
	public class GraphicsResource : GraphicsObject
	{
		private D3D.Resource			resource;
		private D3D.ShaderResourceView	srv;
		private D3D.RenderTargetView	rtv;
		private D3D.DepthStencilView	dsv;
		private D3D.UnorderedAccessView	uav;


		protected GraphicsResource ( GraphicsDevice device, D3D.Resource resource, D3D.ShaderResourceView srv, D3D.RenderTargetView rtv, D3D.DepthStencilView dsv, D3D.UnorderedAccessView uav )	: base( device )
		{
			this.resource	=	resource;
			this.srv		=	srv		;
			this.rtv		=	rtv		;
			this.dsv		=	dsv		;
			this.uav		=	uav		;
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref resource );
				SafeDispose( ref srv );
				SafeDispose( ref rtv );
				SafeDispose( ref dsv );
				SafeDispose( ref uav );
			}

			base.Dispose( disposing );
		}
	}
}
