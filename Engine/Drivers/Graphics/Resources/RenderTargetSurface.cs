using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using Fusion.Core;
using Fusion.Core.Mathematics;

namespace Fusion.Drivers.Graphics {

	/// <summary>
	/// Represenst single rendering surface for render targets.
	/// 
	/// Never dispose RenderTargetSurface. 
	/// It always will be disposed by owning object.
	/// </summary>
	public class RenderTargetSurface : GraphicsObject {

		public int			Width			{ get; private set; }
		public int			Height			{ get; private set; }
		public ColorFormat	Format			{ get; private set; }
		public int			SampleCount		{ get; private set; }

		public Rectangle	Bounds			{ get { return new Rectangle( 0,0, Width, Height ); } }

		public UnorderedAccess UnorderedAccess { get { return unorderedAccess; } }
		UnorderedAccess unorderedAccess;

		internal	RenderTargetView	RTV	=	null;
		internal	Resource			Resource = null;
		internal	int					Subresource = 0;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="rtv"></param>
		internal RenderTargetSurface ( GraphicsDevice device, RenderTargetView rtv, UnorderedAccessView uav, Resource resource, int subresource, ColorFormat format, int width, int height, int sampleCount )
		:base(device)
		{
			Width			=	width;
			Height			=	height;
			Format			=	format;
			SampleCount		=	sampleCount;
			RTV				=	rtv;
			Resource		=	resource;
			Subresource		=	subresource;

			if (uav!=null) 
			{
				unorderedAccess	=	new UnorderedAccess( device, uav );
			}
		}



		/// <summary>
		/// Disposes
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref RTV );
			}

			base.Dispose( disposing );
		}
	}
}
