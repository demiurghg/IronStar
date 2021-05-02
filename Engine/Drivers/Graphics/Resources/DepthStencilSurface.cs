using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using Fusion.Core;
using Fusion.Core.Mathematics;

namespace Fusion.Drivers.Graphics 
{
	public class DepthStencilSurface : ShaderResource 
	{

		public DepthFormat	Format			{ get; private set; }
		public int			SampleCount		{ get; private set; }

		internal	DepthStencilView	DSV	=	null;

		public Rectangle	Bounds			{ get { return new Rectangle( 0,0, Width, Height ); } }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dsv"></param>
		internal DepthStencilSurface ( GraphicsDevice device, DepthStencilView dsv, DepthFormat format, int width, int height, int sampleCount )
		:base(device)
		{
			Width			=	width;
			Height			=	height;
			Format			=	format;
			SampleCount		=	sampleCount;
			DSV				=	dsv;
		}



		/// <summary>
		/// Immediately releases the unmanaged resources used by this object.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref DSV );
			}

			base.Dispose( disposing );
		}
	}
}
