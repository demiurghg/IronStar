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
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Engine.Graphics.Lights;

namespace Fusion.Engine.Graphics 
{
	partial class ShadowMap : DisposableBase 
	{
		readonly GraphicsDevice device;
		readonly RenderSystem rs;
		public const int MaxShadowmapSize	= 8192;
		public readonly QualityLevel ShadowQuality; 

		Allocator2D<IShadowProvider> allocator2;

		public int ShadowMapSize { get { return shadowMapSize; } }
		public int MaxRegionSize { get { return maxRegionSize; } }
		

		/// <summary>
		/// Gets color shadow map buffer.
		/// Actually stores depth value.
		/// </summary>
		public RenderTarget2D ShadowTexture {
			get {
				return shadowMap;
			}
		}


		/// <summary>
		/// Gets color shadow map buffer.
		/// Actually stores depth value.
		/// </summary>
		public ShaderResource ShadowTextureLowRes {
			get {
				return ss.UseHighResFogShadows ? shadowMap : shadowMapLowRes;
			}
		}

		/// <summary>
		/// Gets color shadow map buffer.
		/// Actually stores depth value.
		/// </summary>
		public RenderTarget2D ParticleShadowTexture {
			get {
				return prtShadow;
			}
		}

		/// <summary>
		/// Gets color shadow map buffer.
		/// Actually stores depth value.
		/// </summary>
		public ShaderResource ParticleShadowTextureLowRes {
			get {
				return ss.UseHighResFogShadows ? prtShadow : prtShadowLowRes;
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


		public Allocator2D<IShadowProvider> Allocator { get { return allocator2; } }


		readonly int	shadowMapSize;
		readonly int	maxRegionSize;
		readonly int	minRegionSize;
		readonly ShadowSystem ss;
		DepthStencil2D	depthBuffer;
		RenderTarget2D	shadowMap;
		RenderTarget2D	prtShadow;
		RenderTarget2D	shadowMapLowRes;
		RenderTarget2D	prtShadowLowRes;
		int frameCounter = 0;

		List<Rectangle> dirtyRegionList = new List<Rectangle>();



		/// <summary>
		/// 
		/// </summary>
		/// <param name="singleShadowMapSize"></param>
		/// <param name="splitCount"></param>
		public ShadowMap ( RenderSystem rs, QualityLevel shadowQuality )
		{
			this.ShadowQuality	=	shadowQuality;
			this.device			=	rs.Device;
			this.rs				=	rs;
			this.ss				=	rs.ShadowSystem;

			switch ( shadowQuality ) 
			{
				case QualityLevel.None:		shadowMapSize	=	1024; break;
				case QualityLevel.Low:		shadowMapSize	=	1024; break;
				case QualityLevel.Medium:	shadowMapSize	=	2048; break;
				case QualityLevel.High:		shadowMapSize	=	4096; break;
				case QualityLevel.Ultra:	shadowMapSize	=	8192; break;
				default: throw new ArgumentOutOfRangeException("shadowQuality", "Bad shadow quality");
			}

			maxRegionSize		=	shadowMapSize / 4;
			minRegionSize		=	16;

			allocator2			=	new Allocator2D<IShadowProvider>(shadowMapSize);
			
			depthBuffer			=	new DepthStencil2D( device, DepthFormat.D16,		shadowMapSize,   shadowMapSize   );
			shadowMap			=	new RenderTarget2D( device, ColorFormat.R16_UNorm,	shadowMapSize,   shadowMapSize   );
			prtShadow			=	new RenderTarget2D( device, ColorFormat.Rgba8_sRGB,	shadowMapSize,   shadowMapSize   );
			shadowMapLowRes		=	new RenderTarget2D( device, ColorFormat.R16_UNorm,	shadowMapSize/4, shadowMapSize/4 );
			prtShadowLowRes		=	new RenderTarget2D( device, ColorFormat.Rgba8_sRGB,	shadowMapSize/4, shadowMapSize/4 );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) 
			{
				SafeDispose( ref depthBuffer );
				SafeDispose( ref prtShadow );
				SafeDispose( ref shadowMapLowRes );
				SafeDispose( ref prtShadowLowRes );
			}

			base.Dispose( disposing );
		}


		private void Clear ()
		{
			device.Clear( depthBuffer.Surface, 1, 0 );
			device.Clear( prtShadow.Surface, Color4.White );
		}


		public Vector4 GetScaleOffset ( Rectangle rect )
		{
			return rect.GetMadOpScaleOffsetOffCenterProjectToNDC( shadowMapSize, shadowMapSize );
		}


		int SignedShift ( int value, int shift, int min, int max )
		{
			int result;
			if (shift<0) {
				result	=	value << (-shift);
			} else {
				result	=	value >> (shift);
			}
			return MathUtil.Clamp( result, min, max );
		}


		public int GetShadowRegionSize( int detailLevel )
		{
			return SignedShift( maxRegionSize, detailLevel, minRegionSize, maxRegionSize );
		}


		Rectangle ScaleRectangle( Rectangle r, int s )
		{
			return new Rectangle( r.X / s, r.Y / s, r.Width / s, r.Height / s );
		}


		public void CopyShadowRegionToLowRes( IEnumerable<Rectangle> dirtyRegionList )
		{
			if (dirtyRegionList.Any())
			{
				var scale	=	depthBuffer.Width / shadowMapLowRes.Width;
				var hiRes	=	dirtyRegionList.ToArray();
				var loRes	=	dirtyRegionList.Select( r => ScaleRectangle( r, scale ) ).ToArray();

				rs.Filter2.CopyColorBatched( prtShadowLowRes.Surface, prtShadow, loRes, hiRes, Color.White );
				rs.Filter2.CopyColorBatched( shadowMapLowRes.Surface, shadowMap, loRes, hiRes, Color.White );
			}
		}


		bool NeedCascadeUpdate( ShadowCascade cascade )
		{
			switch (ss.CascadeUpdateMode)
			{
				case CascadeUpdateMode.None: return ShadowInterleave.CascadeInterleaveNone( frameCounter, cascade.Index );
				case CascadeUpdateMode.Interleave1122: return ShadowInterleave.CascadeInterleave1122( frameCounter, cascade.Index );
				case CascadeUpdateMode.Interleave1244: return ShadowInterleave.CascadeInterleave1244( frameCounter, cascade.Index );
			}
			
			return true;
		}


		void ClearShadows()
		{
			dirtyRegionList.Clear();

			device.Clear( depthBuffer.Surface );

			if (ss.ClearEntireShadow)
			{
				device.Clear( shadowMap.Surface, Color4.White );
			}
		}


		public void ClearShadowRegions( IEnumerable<Rectangle> regions )
		{
			device.Clear( depthBuffer.Surface );

			rs.Filter2.ClearColorBatched( shadowMap.Surface, regions.ToArray(), Color.White );
		}
	}
}
