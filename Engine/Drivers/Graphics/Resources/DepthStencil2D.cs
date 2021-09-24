using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Specialized;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using D3D11 = SharpDX.Direct3D11;
using D3D = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using Fusion.Core.Mathematics;


namespace Fusion.Drivers.Graphics 
{
	public class DepthStencil2D : ShaderResource 
	{
		/// <summary>
		/// Samples count
		/// </summary>
		public int			SampleCount { get; private set; }

		/// <summary>
		/// Render target format
		/// </summary>
		public DepthFormat	Format { get; private set; }


		D3D.Texture2D		tex2D;
		DepthStencilSurface	surface;
			

		
		/// <summary>
		/// Creates depth stencil texture, view and shader resource with format D24S8
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="format"></param>
		public DepthStencil2D ( GraphicsDevice device, DepthFormat format, int width, int height, int samples = 1 ) : base( device )
		{
			Log.Trace("DepthStencil2D: f:{0} w:{1} h:{2} s:{3}", format, width, height, samples);
			CheckSamplesCount( samples );

			Width		=	width;
			Height		=	height;
			Depth		=	1;
			Format		=	format;
			SampleCount	=	samples;

			var bindFlags	=	BindFlags.DepthStencil | BindFlags.ShaderResource;


			var	texDesc	=	new Texture2DDescription();
				texDesc.Width				=	width;
				texDesc.Height				=	height;
				texDesc.ArraySize			=	1;
				texDesc.BindFlags			=	bindFlags;
				texDesc.CpuAccessFlags		=	CpuAccessFlags.None;
				texDesc.Format				=	Converter.ConvertToTex( format );
				texDesc.MipLevels			=	1;
				texDesc.OptionFlags			=	ResourceOptionFlags.None;
				texDesc.SampleDescription	=	new DXGI.SampleDescription(samples, 0);
				texDesc.Usage				=	ResourceUsage.Default;

			var dsvDesc	=	new DepthStencilViewDescription();
				dsvDesc.Dimension			=	samples > 1 ? DepthStencilViewDimension.Texture2DMultisampled : DepthStencilViewDimension.Texture2D;
				dsvDesc.Format				=	Converter.ConvertToDSV( format );
				dsvDesc.Flags				=	DepthStencilViewFlags.None;

			var srvDesc	=	new ShaderResourceViewDescription();
				srvDesc.Dimension			=	samples > 1 ? ShaderResourceViewDimension.Texture2DMultisampled : ShaderResourceViewDimension.Texture2D;
				srvDesc.Format				=	Converter.ConvertToSRV( format );
				srvDesc.Texture2D.MostDetailedMip	=	0;
				srvDesc.Texture2D.MipLevels			=	1;

			tex2D		=	new D3D.Texture2D		( device.Device, texDesc );

			var dsv		=	new DepthStencilView	( device.Device, tex2D,	dsvDesc );

			if (bindFlags.HasFlag( BindFlags.ShaderResource)) {
				SRV		=	new ShaderResourceView	( device.Device, tex2D,	srvDesc );
			}

			surface		=	new DepthStencilSurface	( device, dsv, format, width, height, samples );
		}



		/// <summary>
		/// Gets depth stencil surface.
		/// </summary>
		public DepthStencilSurface Surface {
			get {
				return GetSurface();
			}
		}



		/// <summary>
		/// Gets depth stencil surface.
		/// </summary>
		/// <returns></returns>
		public DepthStencilSurface GetSurface()
		{
			return surface;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="targetDepthBuffer"></param>
		public void CopyDepthValuesTo ( DepthStencil2D targetDepthBuffer )
		{
			if (this.Width!=targetDepthBuffer.Width) {
				throw new ArgumentException("CopyDepthValuesTo: source width and destination width are not equal");
			}

			if (this.Height!=targetDepthBuffer.Height) {
				throw new ArgumentException("CopyDepthValuesTo: source height and destination height are not equal");
			}

			device.DeviceContext.CopyResource( this.tex2D, targetDepthBuffer.tex2D );
		}



		/// <summary>
		/// Disposes
		/// </summary>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				Log.Trace("DepthStencil2D: disposing");
				SafeDispose( ref tex2D );
				SafeDispose( ref SRV );
				SafeDispose( ref surface );
			}
			base.Dispose( disposing );
		}
	}
}
