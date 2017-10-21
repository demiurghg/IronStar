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

namespace Fusion.Engine.Graphics {

	partial class ShadowMap : DisposableBase {

		[ShaderStructure]
		struct VPL {
			Vector4 Position;
			Vector4 Normal;
			Vector4 Intensity;
		}

		readonly GraphicsDevice device;
		readonly RenderSystem rs;
		public const int MaxShadowmapSize	= 8192;
		public const int MaxCascades		= 4;
		public readonly QualityLevel ShadowQuality; 

		Allocator2D allocator;


		readonly Cascade[] cascades = new Cascade[MaxCascades];
		

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
			this.rs				=	rs;

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

			colorBuffer		=	new RenderTarget2D( device, ColorFormat.R32F,		shadowMapSize, shadowMapSize );
			depthBuffer		=	new DepthStencil2D( device, DepthFormat.D24S8,		shadowMapSize, shadowMapSize );
			prtShadow		=	new RenderTarget2D( device, ColorFormat.Rgba8_sRGB,	shadowMapSize, shadowMapSize );

			cascades[0]	=	new Cascade(maxRegionSize);
			cascades[1]	=	new Cascade(maxRegionSize);
			cascades[2]	=	new Cascade(maxRegionSize);
			cascades[3]	=	new Cascade(maxRegionSize);
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


		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public Cascade GetCascade ( int index ) 
		{
			if (index<0 || index>=MaxCascades) {
				throw new ArgumentOutOfRangeException("index", "index must be within range 0.." + (MaxCascades-1).ToString() );
			}
			
			return cascades[index];
		}


		Vector4 GetScaleOffset ( Rectangle rect )
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



		/// <summary>
		/// 
		/// </summary>
		/// <param name="allocator"></param>
		/// <param name="visibleSpotLights"></param>
		/// <returns></returns>
		bool AllocateShadowMapRegions ( Allocator2D allocator, int detailBias, IEnumerable<SpotLight> visibleSpotLights )
		{
			foreach ( var cascade in cascades ) {

				Int2 address;

				var size	=	SignedShift( maxRegionSize, cascade.DetailLevel + detailBias, minRegionSize, maxRegionSize );

				if (allocator.TryAlloc( size, "", out address )) {

					var rect	=	new Rectangle( address.X, address.Y, size, size );
					cascade.ShadowRegion		=	rect;
					cascade.ShadowScaleOffset	=	GetScaleOffset( rect );
					
				} else {
					return false;
				}
			}

			foreach ( var light in visibleSpotLights ) {

				Int2 address;

				var size	=	SignedShift( maxRegionSize, light.DetailLevel + detailBias, minRegionSize, maxRegionSize );

				if (allocator.TryAlloc( size, "", out address )) {

					var rect	=	new Rectangle( address.X, address.Y, size, size );
					light.ShadowRegion			=	rect;
					light.ShadowScaleOffset		=	GetScaleOffset( rect );
					
				} else {
					return false;
				}
			}

			return true;
		}



		void ComputeCascadeMatricies ( Camera camera, LightSet lightSet, float splitSize, float splitOffset, float splitFactor, float projDepth )
		{
			var camMatrix	=	camera.GetCameraMatrix( StereoEye.Mono );
			var viewPos		=	camera.GetCameraPosition( StereoEye.Mono );
			var lightDir	=	lightSet.DirectLight.Direction;
			var viewMatrix	=	camera.GetViewMatrix( StereoEye.Mono );

			lightDir.Normalize();

			var slopeBiases = new[] { rs.CSMSlopeBias0, rs.CSMSlopeBias1, rs.CSMSlopeBias2, rs.CSMSlopeBias3 };
			var depthBiases = new[] { rs.CSMDepthBias0, rs.CSMDepthBias1, rs.CSMDepthBias2, rs.CSMDepthBias3 };

			for ( int i = 0; i<cascades.Length; i++ ) {

				var	smSize			=	cascades[i].ShadowRegion.Width; //	width == height

				float	offset		=	splitOffset * (float)Math.Pow( splitFactor, i );
				float	radius		=	splitSize   * (float)Math.Pow( splitFactor, i );

				Vector3 viewDir		=	camMatrix.Forward.Normalized();
				Vector3	origin		=	viewPos + viewDir * offset;

				Matrix	lightRot	=	Matrix.LookAtRH( Vector3.Zero, Vector3.Zero + lightDir, Vector3.UnitY );
				Matrix	lightRotI	=	Matrix.Invert( lightRot );
				Vector3	lsOrigin	=	Vector3.TransformCoordinate( origin, lightRot );
				float	snapValue	=	4 * radius / smSize;

				if (rs.SnapShadowmapCascades) {
					lsOrigin.X		=	(float)Math.Round(lsOrigin.X / snapValue) * snapValue;
					lsOrigin.Y		=	(float)Math.Round(lsOrigin.Y / snapValue) * snapValue;
				}
				//lsOrigin.Z			=	(float)Math.Round(lsOrigin.Z / snapValue) * snapValue;
				origin				=	Vector3.TransformCoordinate( lsOrigin, lightRotI );//*/

				var view			=	Matrix.LookAtRH( origin, origin + lightDir, Vector3.UnitY );
				var projection		=	Matrix.OrthoRH( radius*2, radius*2, -projDepth/2, projDepth/2);
				//var projection		=	Matrix.OrthoRH( radius*2, radius*2, -radius, radius);


				cascades[i].ViewMatrix			=	view;
				cascades[i].ProjectionMatrix	=	projection;	  
				cascades[i].DepthBias			=	depthBiases[i]; // must be zero or epsilon!
				cascades[i].SlopeBias			=	slopeBiases[i]; // must be 1.0f

			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="lightSet"></param>
		public void RenderShadowMaps ( GameTime gameTime, Camera camera, RenderSystem rs, RenderWorld renderWorld, LightSet lightSet )
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

			int detailBias = 0;

			while (true) {

				if (AllocateShadowMapRegions( allocator, detailBias, lights )) {
					break;
				} else {
					allocator.FreeAll();
					Log.Warning("Failed to allocate to much shadow maps. Detail bias {0}. Reallocating.", detailBias);
					detailBias++;
				}
			}

			#warning Configurate or compute values!

			var factor	=	rs.ShadowCascadeFactor;
			var depth	=	rs.ShadowCascadeDepth;
			var size	=	rs.ShadowCascadeSize;

			ComputeCascadeMatricies( camera, lightSet, size, 0, factor, depth );


			//
			//	Render shadow maps regions :
			//
			var instances = renderWorld.Instances;

			using (new PixEvent("Shadow Maps")) {

				device.Clear( depthBuffer.Surface, 1, 0 );
				device.Clear( colorBuffer.Surface, Color4.White );

				foreach ( var cascade in cascades ) {

					var context = new ShadowContext();
					var far		= 1;

					var vp		= new Viewport( cascade.ShadowRegion );

					context.ShadowView			=	cascade.ViewMatrix;
					context.ShadowProjection	=	cascade.ProjectionMatrix;
					context.ShadowViewport		=	vp;
					context.FarDistance			=	far;
					context.SlopeBias			=	cascade.SlopeBias;
					context.DepthBias			=	cascade.DepthBias;
					context.ColorBuffer			=	colorBuffer.Surface;
					context.DepthBuffer			=	depthBuffer.Surface;

					rs.SceneRenderer.RenderShadowMapCascade( context, instances );
				}

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


			//
			//	Particle shadow rendering 
			//
			using ( new PixEvent( "Shadow Masks" ) ) {

				device.Clear( prtShadow.Surface, Color4.Black );

				//	draw cascade shadow particles :
				foreach ( var cascade in cascades ) {

					var context = new ShadowContext();
					var far		= cascade.ProjectionMatrix.GetFarPlaneDistance();

					var vp		= new Viewport( cascade.ShadowRegion );

					rs.Filter2.RenderBorder( prtShadow.Surface, cascade.ShadowRegion, 1 );


				}


				//	draw spot shadow particles :
				foreach ( var spot in lights ) {

					var name	=	spot.SpotMaskName;
					var index	=	lightSet.SpotAtlas.IndexOf( name );

					if (index>=0) {
						var dstRegion	=	spot.ShadowRegion;
						var	srcRegion	=	lightSet.SpotAtlas[index];
						rs.Filter2.RenderQuad( prtShadow.Surface, lightSet.SpotAtlas.Texture.Srv, dstRegion, srcRegion );
					} else {
						var dstRegion	=	spot.ShadowRegion;
						rs.Filter2.RenderSpot( prtShadow.Surface, dstRegion, 1 );
					}
				}


			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="camera"></param>
		/// <param name="rs"></param>
		/// <param name="renderWorld"></param>
		/// <param name="lightSet"></param>
		public void RenderParticleShadows ( GameTime gameTime, Camera camera, RenderSystem rs, RenderWorld renderWorld, LightSet lightSet )
		{
			var lights = lightSet
					.SpotLights
					.Where ( light0 => light0.Visible )
					.ToArray();

			var instances = renderWorld.Instances;


			//
			//	Particle shadow rendering 
			//
			#warning Add shadow mask (from atlas)
			using ( new PixEvent( "Particle Shadows" ) ) {

				//	draw cascade shadow particles :
				foreach ( var cascade in cascades ) {

					var vp		= new Viewport( cascade.ShadowRegion );

					rs.RenderWorld.ParticleSystem.RenderShadow( gameTime, vp, cascade.ViewMatrix, cascade.ProjectionMatrix, prtShadow.Surface, depthBuffer.Surface );

				}


				//	draw spot shadow particles :
				foreach ( var spot in lights ) {

					var context = new ShadowContext();
					var far		= spot.Projection.GetFarPlaneDistance();

					var vp		= new Viewport( spot.ShadowRegion );

					rs.RenderWorld.ParticleSystem.RenderShadow( gameTime, vp, spot.SpotView, spot.Projection, prtShadow.Surface, depthBuffer.Surface );
				}
			}
		}
	}
}
