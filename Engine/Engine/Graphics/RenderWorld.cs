using System;
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

		internal DepthStencil2D		LightProbeDepth;
		internal RenderTargetCube	LightProbeGBuffer0;
		internal RenderTargetCube	LightProbeGBuffer1;
		internal TextureCubeArrayRW	RadianceCache;
		internal TextureCubeArrayRW	RadianceGBuffer0;
		internal TextureCubeArrayRW	RadianceGBuffer1;


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

			LightProbeDepth		=	new DepthStencil2D		( Game.GraphicsDevice, DepthFormat.D24S8,	RenderSystem.LightProbeSize*2, RenderSystem.LightProbeSize*2 );

			#warning false
			LightProbeGBuffer0	=	new RenderTargetCube	( Game.GraphicsDevice, ColorFormat.Rgba8,	RenderSystem.LightProbeSize*2, false ); 
			LightProbeGBuffer1	=	new RenderTargetCube	( Game.GraphicsDevice, ColorFormat.Rgba8,	RenderSystem.LightProbeSize*2, false ); 

			RadianceGBuffer0	=	new TextureCubeArrayRW	( Game.GraphicsDevice, RenderSystem.LightProbeSize*2, RenderSystem.MaxEnvLights, ColorFormat.Rgba8,		false,	RenderSystem.LightProbeBatchSize );
			RadianceGBuffer1	=	new TextureCubeArrayRW	( Game.GraphicsDevice, RenderSystem.LightProbeSize*2, RenderSystem.MaxEnvLights, ColorFormat.Rgba8,		false,	RenderSystem.LightProbeBatchSize );
			RadianceCache		=	new TextureCubeArrayRW	( Game.GraphicsDevice, RenderSystem.LightProbeSize,   RenderSystem.MaxEnvLights, ColorFormat.Rgba16F,	true,	RenderSystem.LightProbeBatchSize );

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

				SafeDispose( ref LightProbeDepth	 );
				SafeDispose( ref LightProbeGBuffer0	 );
				SafeDispose( ref LightProbeGBuffer1	 );
				
				SafeDispose( ref RadianceCache		 );
				SafeDispose( ref RadianceGBuffer0	 );
				SafeDispose( ref RadianceGBuffer1	 );
				
				SafeDispose( ref viewHdrFrame );

			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		public void ClearWorld ()
		{
			LightSet.LightProbes.Clear();
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
			if ( Game.Keyboard.IsKeyDown(Input.Keys.T)) {
				RenderRadiance();
				captureRadiance = false;
			}

			ShowVoxels();

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

				RelightLightProbes();

				//	simulate particles BEFORE lighting
				//	to make particle lit (position is required) and 
				//	get simulated particles for shadows.
				ParticleSystem.Simulate( gameTime, Camera );

				//	prepare light set for shadow rendering :
				rs.LightManager.Update( gameTime, LightSet, Instances );
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
			//rs.Irs.CollectSurfels( Instances );
			//rs.Irs.RenderIRS( Camera, Instances, LightSet );

			//	Z-pass :
			rs.SceneRenderer.RenderZPass( gameTime, stereoEye, Camera, viewHdrFrame, this, false );

			//	Ambient occlusion :
			rs.SsaoFilter.Render( stereoEye, Camera, viewHdrFrame );

			//	Forward+
			rs.SceneRenderer.RenderForward( gameTime, stereoEye, Camera, viewHdrFrame, this, false );

			//	Debug surfels :
			//rs.Irs.DrawDebugSurfels( viewHdrFrame, Camera, stereoEye );


			switch (rs.ShowGBuffer) {
				case 1  : rs.Filter.CopyColor( targetSurface,	viewHdrFrame.Normals ); return;
				case 2  : rs.Filter.CopyAlpha( targetSurface,	viewHdrFrame.GBuffer0 ); return;
				case 3  : rs.Filter.CopyColor( targetSurface,	viewHdrFrame.GBuffer1 ); return;
				case 4  : rs.Filter.CopyAlpha( targetSurface,	viewHdrFrame.GBuffer1 ); return;
				case 5  : rs.Filter.CopyColor( targetSurface,	viewHdrFrame.HdrBuffer ); return;
				case 6  : rs.Filter.Copy( targetSurface,		viewHdrFrame.AOBuffer ); return;
				case 7  : rs.Filter.StretchRect( targetSurface, rs.LightManager.ShadowMap.ParticleShadow ); return;
				case 8  : rs.Filter.StretchRect( targetSurface, rs.LightManager.ShadowMap.ColorBuffer ); return;
				case 9  : rs.Filter.StretchRect( targetSurface, ParticleSystem.SoftStream.Lightmap ); return;
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
			//rs.Fog.RenderFog( Camera, LightSet, FogSettings );

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
		public void ShowVoxels ()
		{
			if (!Game.Keyboard.IsKeyDown(Keys.P)) {
				return;
			}

			int count = 0;

			foreach ( var instance in Instances ) {

				if (instance.Mesh==null) {
					continue;
				}

				foreach ( var tri in instance.Mesh.Triangles ) {

					var p0	=	Vector3.TransformCoordinate( instance.Mesh.Vertices[ tri.Index0 ].Position, instance.World ) * 2;
					var p1	=	Vector3.TransformCoordinate( instance.Mesh.Vertices[ tri.Index1 ].Position, instance.World ) * 2;
					var p2	=	Vector3.TransformCoordinate( instance.Mesh.Vertices[ tri.Index2 ].Position, instance.World ) * 2;

					/*Debug.DrawLine( p0, p1, Color.Red );
					Debug.DrawLine( p1, p2, Color.Red );
					Debug.DrawLine( p2, p0, Color.Red );*/

					Voxelizer.RasterizeTriangle( p0, p1, p2, (x,y,z) => {
						if (x<-32 || x>31) return;											
						if (y<-32 || y>31) return;											
						if (z<-32 || z>31) return;											

						x	=	(float)Math.Round(x)/2;													
						y	=	(float)Math.Round(y)/2;													
						z	=	(float)Math.Round(z)/2;		
						Debug.DrawBox( new Vector3(x,y,z), 0.2f, Color.Red );
					});
				}

			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public void RenderRadiance ()
		{
			var sw = new Stopwatch();
			var device	=	Game.GraphicsDevice;

			device.ResetStates();

			Log.Message("Radiance geometry capture...");

			var skyAmbient = SkySettings.AmbientLevel;

			sw.Start();
			using (new PixEvent("Capture Radiance Geometry")) {

				foreach ( var lightProbe in LightSet.LightProbes ) {

					for (int i=0; i<6; i++) {

						var face	=	(CubeFace)i;
						var depth	=	LightProbeDepth.Surface;
						var gbuf0	=	LightProbeGBuffer0.GetSurface( 0, face );
						var gbuf1	=	LightProbeGBuffer1.GetSurface( 0, face );
					
						device.Clear( depth );
						device.Clear( gbuf0, Color4.Black );
						device.Clear( gbuf1, Color4.Black );

						var context	=	new LightProbeContext( lightProbe, face, depth, gbuf0, gbuf1 );

						//	render g-buffer :
						rs.SceneRenderer.RenderLightProbeGBuffer( context, this, Instances );
					}
				
					RadianceGBuffer0.CopyFromRenderTargetCube( lightProbe.ImageIndex, LightProbeGBuffer0 );
					RadianceGBuffer1.CopyFromRenderTargetCube( lightProbe.ImageIndex, LightProbeGBuffer1 );

					rs.LightManager.RelightLightProbe( RadianceGBuffer0, RadianceGBuffer1, lightProbe, LightSet, skyAmbient, RadianceCache );
				}

				sw.Stop();
			}

			Log.Message("{0} light probes - {1} ms", LightSet.LightProbes.Count, sw.ElapsedMilliseconds);
		}



		int lightProbeUpdateCounter = 0;

		/// <summary>
		/// 
		/// </summary>
		void RelightLightProbes ()
		{
			var sw = new Stopwatch();
			sw.Start();

			using (new PixEvent("Light probe relighting")) {

				var skyAmbient = SkySettings.AmbientLevel;
				this.rs.Device.ResetStates();

				foreach ( var lightProbe in LightSet.LightProbes ) {

					rs.LightManager.RelightLightProbe( RadianceGBuffer0, RadianceGBuffer1, lightProbe, LightSet, skyAmbient, RadianceCache );
				}
			}

			rs.LightManager.PrefilterLightProbes( LightSet, RadianceCache, lightProbeUpdateCounter );
			lightProbeUpdateCounter++;

			sw.Stop();
			//Log.Message("Relight light probes [CPU] : {0} light probes - {1} ms", LightSet.LightProbes.Count, sw.ElapsedMilliseconds);
		}



		void BuildVisibility ()
		{
		}
	}
}
