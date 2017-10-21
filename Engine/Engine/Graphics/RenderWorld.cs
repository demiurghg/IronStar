﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Engine.Input;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using System.Diagnostics;
using Fusion.Core.Shell;

namespace Fusion.Engine.Graphics {

	/// <summary>
	/// Represents entire visible world.
	/// </summary>
	public class RenderWorld : DisposableBase {

		readonly Game			Game;
		readonly RenderSystem	rs;

		/// <summary>
		/// Gets and sets view's camera.
		/// This value is already initialized when View object is created.
		/// </summary>
		public Camera Camera {
			get; set;
		}


		/// <summary>
		/// Gets and sets weapon view camera.
		/// This value is already initialized when View object is created.
		/// </summary>
		public Camera WeaponCamera {
			get; set;
		}

		/// <summary>
		/// Indicated whether target buffer should be cleared before rendering.
		/// </summary>
		public bool Clear {	
			get; set;
		}

		/// <summary>
		/// Gets and sets clear color
		/// </summary>
		public Color4 ClearColor {
			get; set;
		}



		/// <summary>
		/// Gets HDR settings.
		/// </summary>
		public HdrSettings HdrSettings {
			get; private set;
		}

		/// <summary>
		/// Gets sky settings.
		/// </summary>
		public SkySettings SkySettings {
			get; private set;
		}

		/// <summary>
		/// Gets sky settings.
		/// </summary>
		public FogSettings FogSettings {
			get; private set;
		}

		/// <summary>
		/// Gets sky settings.
		/// </summary>
		public DofSettings DofSettings {
			get; private set;
		}

		/// <summary>
		/// Gets view light set.
		/// This value is already initialized when View object is created.
		/// </summary>
		public LightSet LightSet {
			get; private set;
		}

		/// <summary>
		/// Gets debug render
		/// </summary>
		public DebugRender Debug {
			get { return debug; }
		}
		DebugRender debug;

		/// <summary>
		/// Gets particle system instance.
		/// </summary>
		public ParticleSystem ParticleSystem {
			get { return particleSystem; }
		}
		ParticleSystem	particleSystem;

		/// <summary>
		/// Gets collection of mesh instances.
		/// </summary>
		public ICollection<MeshInstance> Instances {
			get; private set;
		}


		VirtualTexture virtualTexture = null;

		/// <summary>
		/// Sets and gets virtual texture for entire world
		/// </summary>
		public VirtualTexture VirtualTexture {
			get {
				return virtualTexture;
			}
			set {
				if (value==null) {
					rs.VTSystem.Stop();
					virtualTexture = value;
				} else {
					if (virtualTexture!=value) {
						rs.VTSystem.Stop();
						rs.VTSystem.Start(value);
						virtualTexture = value;
					}
				}
			}
		}


		HdrFrame viewHdrFrame;
		HdrFrame radianceFrame;

		internal RenderTargetCube Radiance;
		internal TextureCubeArray RadianceCache;


		/// <summary>
		/// Creates ViewLayerHDR instance
		/// </summary>
		/// <param name="Game">Game engine</param>
		/// <param name="width">Target width.</param>
		/// <param name="height">Target height.</param>
		public RenderWorld ( Game game, int width, int height )
		{
			Game		=	game;
			this.rs		=	Game.RenderSystem;

			Camera		=	new Camera();
			WeaponCamera=	new Camera();

			var vp	=	Game.GraphicsDevice.DisplayBounds;

			if (width<=0) {
				width	=	vp.Width;
			}
			if (height<=0) {
				height	=	vp.Height;
			}

			HdrSettings		=	new HdrSettings();
			SkySettings		=	new SkySettings();
			DofSettings		=	new DofSettings();
			FogSettings		=	new FogSettings();

			Instances		=	new List<MeshInstance>();
			LightSet		=	new LightSet( Game.RenderSystem );

			debug			=	new DebugRender( Game );
			
			particleSystem	=	new ParticleSystem( Game.RenderSystem, this );

			radianceFrame	=	new HdrFrame( Game, 512,512 );

			Radiance		=	new RenderTargetCube( Game.GraphicsDevice, ColorFormat.Rgba16F, RenderSystem.EnvMapSize, true );
			RadianceCache	=	new TextureCubeArray( Game.GraphicsDevice, 128, RenderSystem.MaxEnvLights, ColorFormat.Rgba16F, true );

			Resize( width, height );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				
				SafeDispose( ref particleSystem );

				SafeDispose( ref debug );

				SafeDispose( ref Radiance );
				SafeDispose( ref RadianceCache );

				SafeDispose( ref viewHdrFrame );
				SafeDispose( ref radianceFrame );

			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		public void ClearWorld ()
		{
			LightSet.EnvLights.Clear();
			LightSet.OmniLights.Clear();
			LightSet.SpotLights.Clear();
			LightSet.SpotAtlas	=	null;
			LightSet.Decals.Clear();

			Instances.Clear();

			//	immediate?
			ParticleSystem.KillParticles();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void Resize ( int newWidth, int newHeight )
		{
			SafeDispose( ref viewHdrFrame );

			//	clamp values :
			newWidth	=	Math.Max(128, newWidth);
			newHeight	=	Math.Max(128, newHeight);

			int targetWidth		=	newWidth;
			int targetHeight	=	newHeight;

			int bloomWidth		=	( targetWidth/2  ) & 0xFFF0;
			int bloomHeight		=	( targetHeight/2 ) & 0xFFF0;

			viewHdrFrame		=	new HdrFrame ( Game, targetWidth, targetHeight );
		}





		/// <summary>
		/// Indicates whether world is paused.
		/// </summary>
		public bool IsPaused {
			get {
				return isPaused;
			}
			set {	
				if (isPaused!=value) {
					isPaused = value;
				}
			}
		}


		bool isPaused;


		/// <summary>
		/// Pauses render world simulation and animation.
		/// </summary>
		public void Pause ()
		{
			IsPaused	=	true;
		}



		/// <summary>
		/// Resumes render world simulation and animation.
		/// </summary>
		public void Resume ()
		{
			IsPaused	=	false;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		[Obsolete]
		public IEnumerable<MeshInstance> CreateModelInstances ()
		{
			throw new NotImplementedException();
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Rendering :
		 * 
		-----------------------------------------------------------------------------------------*/
		bool captureRadiance;

		public void CaptureRadiance ()
		{
			captureRadiance = true;
		}





		/// <summary>
		/// Renders view
		/// </summary>
		internal void Render ( GameTime gameTime, StereoEye stereoEye, RenderTargetSurface targetSurface )
		{
			if ( captureRadiance ) {
				RenderRadiance();
				captureRadiance = false;
			}

			ShowSurfels();

			//	clear target buffer if necassary :
			if ( Clear) {
				rs.Device.Clear( targetSurface, ClearColor );
			}

			var viewport	=	new Viewport( 0,0, targetSurface.Width, targetSurface.Height );

			//	Render HDR stuff: mesh instances, 
			//	special effects, sky, water, light etc. 
			RenderHdrScene( gameTime, stereoEye, viewport, targetSurface );

			//	fill alpha with one value :
			rs.Filter.FillAlphaOne( targetSurface );
		}



		/// <summary>
		/// 
		/// </summary>
		void ClearBuffers ( HdrFrame frame )
		{
			Game.GraphicsDevice.Clear( frame.GBuffer0.Surface,			Color4.Black );
			Game.GraphicsDevice.Clear( frame.GBuffer1.Surface,			Color4.Black );

			Game.GraphicsDevice.Clear( frame.FeedbackBufferRB.Surface,	Color4.Zero );

			Game.GraphicsDevice.Clear( frame.FeedbackBuffer.Surface,	Color4.Zero );

			Game.GraphicsDevice.Clear( frame.DepthBuffer.Surface,		1, 0 );
			Game.GraphicsDevice.Clear( frame.HdrBuffer.Surface,			Color4.Black );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="stereoEye"></param>
		void RenderHdrScene ( GameTime gameTime, StereoEye stereoEye, Viewport viewport, RenderTargetSurface targetSurface )
		{
			//	clear g-buffer and hdr-buffers:
			ClearBuffers( viewHdrFrame );

			//	single pass for stereo rendering :
			if (stereoEye!=StereoEye.Right) {

				//	simulate particles BEFORE lighting
				//	to make particle lit (position is required) and 
				//	get simulated particles for shadows.
				ParticleSystem.Simulate( gameTime, Camera );

				//	prepare light set for shadow rendering :
				rs.LightManager.Update( gameTime, LightSet );
				rs.LightManager.LightGrid.UpdateLightSetVisibility( stereoEye, Camera, LightSet );

				//	allocated and render shadows :
				rs.LightManager.ShadowMap.RenderShadowMaps( gameTime, Camera, rs, this, LightSet );

				//	clusterize light set :
				rs.LightManager.LightGrid.ClusterizeLightSet( stereoEye, Camera, LightSet );

				//	render particle lighting :
				ParticleSystem.RenderLight( gameTime, Camera );

				//	render particles casting shadows :
				rs.LightManager.ShadowMap.RenderParticleShadows( gameTime, Camera, rs, this, LightSet );
			}

			//	Render GI :
			rs.SceneRenderer.RenderVoxelGrid( this ); 
			rs.Lpv.RenderLpv( Camera, LightSet );

			//	Z-pass :
			rs.SceneRenderer.RenderZPass( gameTime, stereoEye, Camera, viewHdrFrame, this, false );

			//	Ambient occlusion :
			rs.SsaoFilter.Render( stereoEye, Camera, viewHdrFrame );

			//	Forward+
			rs.SceneRenderer.RenderForward( gameTime, stereoEye, Camera, viewHdrFrame, this, false );


			switch (rs.ShowGBuffer) {
				case 1  : rs.Filter.CopyColor( targetSurface,	viewHdrFrame.Normals ); return;
				case 2  : rs.Filter.CopyAlpha( targetSurface,	viewHdrFrame.GBuffer0 ); return;
				case 3  : rs.Filter.CopyColor( targetSurface,	viewHdrFrame.GBuffer1 ); return;
				case 4  : rs.Filter.CopyAlpha( targetSurface,	viewHdrFrame.GBuffer1 ); return;
				case 5  : rs.Filter.CopyColor( targetSurface,	viewHdrFrame.HdrBuffer ); return;
				case 6  : rs.Filter.Copy( targetSurface,		viewHdrFrame.AOBuffer ); return;
				case 7  : rs.Filter.StretchRect( targetSurface, rs.LightManager.ShadowMap.ParticleShadow ); return;
				case 8  : rs.Filter.StretchRect( targetSurface, rs.LightManager.ShadowMap.ColorBuffer ); return;
				case 9  : rs.Filter.StretchRect( targetSurface, ParticleSystem.Lightmap ); return;
				case 10 : rs.Filter.StretchRect( targetSurface, viewHdrFrame.FeedbackBufferRB, SamplerState.PointClamp ); return;
			}

			if (rs.VTSystem.ShowPhysicalTextures) {
				rs.Filter.StretchRect( targetSurface, rs.VTSystem.PhysicalPages0 );
				return;
			}
			if (rs.VTSystem.ShowPageTexture) {
				rs.Filter.Copy( targetSurface, rs.VTSystem.PageTable );
				return;
			}

			//	render sky :
			rs.Sky.Render( Camera, stereoEye, viewHdrFrame, SkySettings );
			rs.Sky.RenderFogTable( SkySettings );

			//	render fog :
			rs.Fog.RenderFog( Camera, LightSet, FogSettings );

			//	render lights :
			//rs.LightRenderer.RenderLighting( stereoEye, Camera, viewHdrFrame, this, Radiance );

			//	render "solid" DOF :
			rs.DofFilter.Render( gameTime, viewHdrFrame.LightAccumulator, viewHdrFrame.HdrBuffer, viewHdrFrame.DepthBuffer, this );

			//	render particles :
			ParticleSystem.Render( gameTime, Camera, stereoEye, viewHdrFrame );

			//	apply tonemapping and bloom :
			rs.HdrFilter.Render( gameTime, HdrSettings, this.viewHdrFrame );


			//	apply FXAA
			if (rs.UseFXAA) {
				rs.Filter.Fxaa( targetSurface, viewHdrFrame.FinalColor );
			} else {
				rs.Filter.Copy( targetSurface, viewHdrFrame.FinalColor );
			} 

			//	draw debug lines :
			Debug.Render( targetSurface, viewHdrFrame.DepthBuffer.Surface, Camera );
		}




		/// <summary>
		/// 
		/// </summary>
		public void ShowSurfels ()
		{
			if (!Game.Keyboard.IsKeyDown(Keys.P)) {
				return;
			}

			int count = 0;

			foreach ( var instance in Instances ) {

				if (instance.Mesh==null) {
					continue;
				}

				if (!instance.Mesh.Surfels.Any()) {
					instance.Mesh.BuildSurfels(1.0f);
				}

				foreach ( var surf in instance.Mesh.Surfels ) {
					var p = Vector3.TransformCoordinate	( surf.Position, instance.World );
					var n = Vector3.TransformNormal		( surf.Normal,	 instance.World );
					var r = (float)Math.Sqrt(surf.Area / 3.141592f);
					Debug.DrawPoint( p + n * 0.1f, 0.05f, Color.LightGray, 1 );
					Debug.DrawVector( p, n, Color.LightGray, 0.1f );
					Debug.DrawAxialRing( p, n, r, Color.LightGray );
				}

				count += instance.Mesh.Surfels.Count;
			}

			Log.Message("{0} surfels", count );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public void RenderRadiance ()
		{
			var sw = new Stopwatch();

			Log.Message("Radiance capture...");

			sw.Start();
			using (new PixEvent("Capture Radiance")) {

				var sun	=	SkySettings.SunGlowIntensity;
				SkySettings.SunGlowIntensity = 0;

				int index = 0;

				foreach ( var envLight in LightSet.EnvLights ) {

					for (int i=0; i<6; i++) {
					
						ClearBuffers( radianceFrame );

						var camera = new Camera();
						camera.SetupCameraCubeFace( envLight.Position, (CubeFace)i, 0.125f, 5000 );

						//	render g-buffer :
						rs.SceneRenderer.RenderForward( new GameTime(0,0,0), StereoEye.Mono, camera, radianceFrame, this, true );

						//	render sky :
						rs.Sky.Render( camera, StereoEye.Mono, radianceFrame, SkySettings );

						//	downsample captured frame to cube face.
						rs.Filter.StretchRect4x4( Radiance.GetSurface( 0, (CubeFace)i ), radianceFrame.HdrBuffer, SamplerState.LinearClamp, true );
					}
				
					//	prefilter cubemap :
					rs.Filter.PrefilterEnvMap( Radiance );

					RadianceCache.CopyFromRenderTargetCube( index, Radiance );
					index ++;
				}
				sw.Stop();
	
				SkySettings.SunGlowIntensity = sun;
			}

			Log.Message("{0} light probes - {1} ms", LightSet.EnvLights.Count, sw.ElapsedMilliseconds);
		}





		void BuildVisibility ()
		{
		}
	}
}
