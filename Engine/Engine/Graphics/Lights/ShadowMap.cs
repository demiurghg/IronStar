using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using Fusion.Build.Mapping;


namespace Fusion.Engine.Graphics {

	class ShadowMap : DisposableBase {

		readonly GraphicsDevice device;
		public const int MaxShadowmapSize	= 8192;
		public readonly QualityLevel ShadowQuality; 

		Allocator2D allocator;

		

		/// <summary>
		/// Gets color shadow map buffer.
		/// Actually stores depth value.
		/// </summary>
		public RenderTarget2D ColorBuffer {
			get {
				return colorBuffer;
			}
		}



		/// <summary>
		/// Gets color shadow map buffer.
		/// Actually stores depth value.
		/// </summary>
		public RenderTarget2D ParticleShadow {
			get {
				return prtShadow;
			}
		}



		/// <summary>
		/// Gets color shadow map buffer.
		/// </summary>
		public DepthStencil2D DepthBuffer {
			get {
				return depthBuffer;
			}
		}


		readonly int	shadowMapSize;
		readonly int	maxRegionSize;
		readonly int	minRegionSize;
		DepthStencil2D	depthBuffer;
		RenderTarget2D	colorBuffer;
		RenderTarget2D	prtShadow;



		/// <summary>
		/// 
		/// </summary>
		/// <param name="singleShadowMapSize"></param>
		/// <param name="splitCount"></param>
		public ShadowMap ( RenderSystem rs, QualityLevel shadowQuality )
		{
			this.ShadowQuality	=	shadowQuality;
			this.device			=	rs.Device;

			switch ( shadowQuality ) {
				case QualityLevel.None:		shadowMapSize	=	1024; break;
				case QualityLevel.Low:		shadowMapSize	=	1024; break;
				case QualityLevel.Medium:	shadowMapSize	=	2048; break;
				case QualityLevel.High:		shadowMapSize	=	4096; break;
				case QualityLevel.Ultra:	shadowMapSize	=	8192; break;
				default: throw new ArgumentOutOfRangeException("shadowQuality", "Bad shadow quality");
			}

			maxRegionSize	=	shadowMapSize / 4;
			minRegionSize	=	16;

			allocator	=	new Allocator2D(shadowMapSize);

			colorBuffer	=	new RenderTarget2D( device, ColorFormat.R32F,		shadowMapSize, shadowMapSize );
			depthBuffer	=	new DepthStencil2D( device, DepthFormat.D24S8,		shadowMapSize, shadowMapSize );
			prtShadow	=	new RenderTarget2D( device, ColorFormat.Rgba8_sRGB,	shadowMapSize, shadowMapSize );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref colorBuffer );
				SafeDispose( ref depthBuffer );
				SafeDispose( ref prtShadow );
			}

			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		public void Clear ()
		{
			device.Clear( depthBuffer.Surface, 1, 0 );
			device.Clear( colorBuffer.Surface, Color4.White );
			device.Clear( prtShadow.Surface, Color4.White );
		}


		public Vector4 GetScaleOffset ( Rectangle rect )
		{
			float size = shadowMapSize;
			float ax = rect.Width  / size;
			float ay = rect.Height / size;
			float bx = rect.Left   / size;
			float by = rect.Top    / size;

			float x		=	0.5f * ax;
			float y		=  -0.5f * ay;
			float z		=   0.5f * ax + bx;
			float w		=	0.5f * ay + by;

			return new Vector4(x,y,z,w);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="lightSet"></param>
		public void RenderShadowMaps ( RenderSystem rs, RenderWorld renderWorld, LightSet lightSet )
		{
			//
			//	Allocate shadow map regions :
			//
			allocator.FreeAll();

			var lights = lightSet
					.SpotLights
					.Where ( light0 => light0.Visible )
					.OrderBy( light1 => light1.DetailLevel )
					.ToArray();

			int fallbackBias = 0;

			foreach ( var light in lights ) {

				#warning Fallback on 
				var size	=	MathUtil.Clamp( maxRegionSize >> (light.DetailLevel+fallbackBias), minRegionSize, maxRegionSize );
				var addr	=	allocator.Alloc( size, "" );
				var rect	=	new Rectangle( addr.X, addr.Y, size, size );

				light.ShadowRegion			=	rect;
				light.ShadowScaleOffset		=	GetScaleOffset( rect );
			}


			//
			//	Render shadow maps regions :
			//
			var instances = renderWorld.Instances;

			using (new PixEvent("Spotlight Shadow Maps")) {

				device.Clear( depthBuffer.Surface, 1, 0 );
				device.Clear( colorBuffer.Surface, Color4.White );

				foreach ( var spot in lights ) {

					var context = new ShadowContext();
					var far		= spot.Projection.GetFarPlaneDistance();

					var vp		= new Viewport( spot.ShadowRegion );

					context.ShadowView			=	spot.SpotView;
					context.ShadowProjection	=	spot.Projection;
					context.ShadowViewport		=	vp;
					context.FarDistance			=	far;
					context.SlopeBias			=	spot.SlopeBias;
					context.DepthBias			=	spot.DepthBias;
					context.ColorBuffer			=	colorBuffer.Surface;
					context.DepthBuffer			=	depthBuffer.Surface;

					rs.SceneRenderer.RenderShadowMapCascade( context, instances );
				}
			}
		}
	}
}
