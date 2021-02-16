using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Core.Input;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using System.Diagnostics;
using Fusion.Core.Shell;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics.Lights;
using Fusion.Build;
using Fusion.Engine.Graphics.GI;
using Fusion.Engine.Graphics.Bvh;
using Fusion.Engine.Graphics.GI2;

namespace Fusion.Engine.Graphics {

	/// <summary>
	/// Represents entire visible world.
	/// </summary>
	public class RenderWorld : DisposableBase {

		readonly Game			Game;
		readonly RenderSystem	rs;

		Camera camera;
		Camera weaponCamera;
		Camera shadowCamera;
		Camera cubemapCamera;

		/// <summary>
		/// Gets view's camera.
		/// This value is already initialized when View object is created.
		/// </summary>
		public Camera Camera {
			get { return camera; }
		}


		/// <summary>
		/// Gets weapon view camera.
		/// This value is already initialized when View object is created.
		/// </summary>
		public Camera WeaponCamera {
			get { return weaponCamera; }
		}


		/// <summary>
		/// Gets weapon view camera.
		/// This value is already initialized when View object is created.
		/// </summary>
		public Camera ShadowCamera {
			get { return shadowCamera; }
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
		public ICollection<RenderInstance> Instances {
			get; private set;
		}

		/// <summary>
		/// Gets collection of mesh instances.
		/// </summary>
		//public ICollection<MeshInstanceGroup> InstanceGroups {
		//	get; private set;
		//}


		VirtualTexture		virtualTexture = null;
		LightMap			irradianceMap = null;	
		ILightProbeProvider	lightProveProvider = null;

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


		/// <summary>
		/// Sets anf gets irradiance map
		/// </summary>
		public ILightProbeProvider LightProbeProvider 
		{
			get 
			{
				return lightProveProvider;
			}
			set 
			{
				if (lightProveProvider!=value) 
				{
					lightProveProvider = value;
				}
			}
		}


		HdrFrame viewHdrFrame;

		internal HdrFrame HdrFrame { get { return viewHdrFrame; } }

		//internal DepthStencil2D		LightProbeDepth;
		//internal RenderTargetCube	LightProbeHdr;
		//internal RenderTargetCube	LightProbeHdrTemp;




		/// <summary>
		/// Creates ViewLayerHDR instance
		/// </summary>
		/// <param name="Game">Game engine</param>
		/// <param name="width">Target width.</param>
		/// <param name="height">Target height.</param>
		public RenderWorld ( Game game, int width, int height )
		{
			Game			=	game;
			this.rs			=	Game.RenderSystem;

			camera			=	new Camera(rs, nameof(camera));
			weaponCamera	=	new Camera(rs, nameof(weaponCamera));
			shadowCamera	=	new Camera(rs, nameof(shadowCamera));

			var vp	=	Game.GraphicsDevice.DisplayBounds;

			if (width<=0) {
				width	=	vp.Width;
			}
			if (height<=0) {
				height	=	vp.Height;
			}

			Instances		=	new List<RenderInstance>();
			LightSet		=	new LightSet( Game.RenderSystem );

			debug			=	new DebugRender( Game );
			
			particleSystem	=	new ParticleSystem( Game.RenderSystem, this );

			//LightProbeDepth		=	new DepthStencil2D		( Game.GraphicsDevice, DepthFormat.D24S8,	RenderSystem.LightProbeSize, RenderSystem.LightProbeSize );
			//LightProbeHdr		=	new RenderTargetCube	( Game.GraphicsDevice, ColorFormat.Rgba16F,	RenderSystem.LightProbeSize, RenderSystem.LightProbeMaxMips ); 
			//LightProbeHdrTemp	=	new RenderTargetCube	( Game.GraphicsDevice, ColorFormat.Rgba16F,	RenderSystem.LightProbeSize, RenderSystem.LightProbeMaxMips ); 

			Resize( width, height );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {

				SafeDispose( ref camera );
				SafeDispose( ref weaponCamera );
				SafeDispose( ref shadowCamera );
				SafeDispose( ref cubemapCamera );
				
				SafeDispose( ref particleSystem );

				SafeDispose( ref debug );

				//SafeDispose( ref LightProbeDepth	);
				//SafeDispose( ref LightProbeDepth	);
				//SafeDispose( ref LightProbeHdr		);
				
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
		void UpdateInstanceAndLightMapping ()
		{
			if (rs.Radiosity.LightMap!=null) 
			{
				foreach ( var instance in Instances ) 
				{
					if (instance.Group==InstanceGroup.Static) 
					{
						instance.LightMapScaleOffset = rs.Radiosity.LightMap.GetRegionMadST( instance.LightMapRegionName );
					}
				}
			}

			if (lightProveProvider!=null) 
			{
				foreach ( var lpb in LightSet.LightProbes ) 
				{
					lpb.ImageIndex	= lightProveProvider.GetLightProbeIndex( lpb.Guid.ToString() );
				}
			}
		}
	

		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Visibility system :
		 * 
		-----------------------------------------------------------------------------------------*/

		RenderList	rlMainView = new RenderList(5000);

		BvhTree<RenderInstance> sceneBvhTree = null;

		public BvhTree<RenderInstance> SceneBvhTree { get { return sceneBvhTree; } }


		Color DebugBBoxColor( BoundingBox box )
		{
			var containment = camera.Frustum.Contains( box );
			switch (containment)
			{
				case ContainmentType.Disjoint: return Color.Red;
				case ContainmentType.Intersects: return Color.Blue;
				case ContainmentType.Contains: return Color.Lime;
			}
			return Color.Black;
		}


		void UpdateVisibility()
		{
			sceneBvhTree	=	new BvhTree<RenderInstance>( 
				Instances, //.Where( inst0 => inst0.Group!=InstanceGroup.Weapon ), 
				inst1 => inst1.ComputeWorldBoundingBox(), 
				inst2 => inst2.World.TranslationVector );

			if (rs.LockVisibility) return;

			if (rs.ShowBoundingBoxes)
			{
				sceneBvhTree.Traverse( (inst,bbox) => Debug.DrawBox( bbox, DebugBBoxColor(bbox) ) );
			}

			if (rs.SkipFrustumCulling)
			{
				rlMainView.Clear();
				rlMainView.AddRange( Instances );
			}
			else
			{
				rlMainView.Clear();
				rlMainView.AddRange( sceneBvhTree.Traverse( (bbox) => camera.Frustum.Contains( bbox ) ) );
			}
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Rendering :
		 * 
		-----------------------------------------------------------------------------------------*/


		/// <summary>
		/// Renders view
		/// </summary>
		internal void Render ( GameTime gameTime, StereoEye stereoEye, RenderTargetSurface targetSurface )
		{
			//	clear target buffer if necassary :
			if ( Clear) {
				rs.Device.Clear( targetSurface, ClearColor );
			}

			var viewport	=	new Viewport( 0,0, targetSurface.Width, targetSurface.Height );

			UpdateInstanceAndLightMapping();

			UpdateVisibility();

			//	Render HDR stuff: mesh instances, 
			//	special effects, sky, water, light etc. 
			RenderHdrScene( gameTime, stereoEye, viewport, targetSurface );

			//	fill alpha with one value :
			rs.Filter.FillAlphaOne( targetSurface );

			//	update camera history matricies :
			Camera.UpdateHistory(gameTime);
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="stereoEye"></param>
		void RenderHdrScene ( GameTime gameTime, StereoEye stereoEye, Viewport viewport, RenderTargetSurface targetSurface )
		{
			using ( new PixEvent( "Frame" ) ) {
				//	clear g-buffer and hdr-buffers:
				viewHdrFrame.Clear();

				using ( new PixEvent( "Frame Preprocessing" ) ) {

					//	single pass for stereo rendering :
					if ( stereoEye!=StereoEye.Right ) {

						//	simulate particles BEFORE lighting
						//	to make particle lit (position is required) and 
						//	get simulated particles for shadows.
						ParticleSystem.Simulate( gameTime, Camera );

						//	prepare light set for shadow rendering :
						rs.LightManager.Update( gameTime, LightSet, Instances );
						rs.LightManager.LightGrid.UpdateLightSetVisibility( stereoEye, Camera, LightSet );

						//	allocated and render shadows :
						rs.LightManager.ShadowMap.RenderShadowMaps( gameTime, Camera, rs, this, LightSet );

						//	render sky-cube
						rs.Sky.RenderSkyCube();

						//	clusterize light set :
						rs.LightManager.LightGrid.ClusterizeLightSet( stereoEye, Camera, LightSet );

						//  compute radiosity using shadowmaps
						rs.Radiosity.Render( gameTime );

						//	relight cubemaps :
						LightProbeProvider?.Update( LightSet, Camera );

						//	render particle lighting :
						ParticleSystem.RenderLight( gameTime, Camera );

						//	render particles casting shadows :
						rs.LightManager.ShadowMap.RenderParticleShadows( gameTime, rs, this, LightSet );
					}
				}


				using ( new PixEvent( "Frame Scene Rendering" ) ) 
				{
					//	render sky and fog :
					rs.Sky.RenderSkyLut( gameTime, Camera );
					rs.Fog.RenderFogVolume( Camera, LightSet );
					rs.Sky.RenderSky( gameTime, Camera, stereoEye, viewHdrFrame );
					rs.Sky.RenderSkyCube( gameTime, Camera );

					//	Z-pass without weapon :
					rs.SceneRenderer.RenderZPass( gameTime, stereoEye, Camera, viewHdrFrame, rlMainView, InstanceGroup.NotWeapon );

					//	Ambient occlusion :
					rs.SsaoFilter.Render( stereoEye, Camera, viewHdrFrame );

					//	Z-pass weapon :
					rs.SceneRenderer.RenderZPass( gameTime, stereoEye, WeaponCamera, viewHdrFrame, rlMainView, InstanceGroup.Weapon );

					//------------------------------------------------------------
					//	Forward+
					rs.SceneRenderer.RenderForwardSolid( gameTime, stereoEye, Camera		, viewHdrFrame, rlMainView, InstanceGroup.NotWeapon );
					rs.SceneRenderer.RenderForwardSolid( gameTime, stereoEye, WeaponCamera	, viewHdrFrame, rlMainView, InstanceGroup.Weapon );

					rs.LightMapDebugger.Render( Camera, viewHdrFrame );

					ParticleSystem.RenderHard( gameTime, Camera, stereoEye, viewHdrFrame );

					using ( new PixEvent( "Background downsample" ) ) {
						var hdrFrame = viewHdrFrame;
						var filter	 = rs.Filter;
						var blur	 = rs.Blur;
						filter.StretchRect( hdrFrame.Bloom0.Surface, hdrFrame.HdrTarget, SamplerState.LinearClamp );
						hdrFrame.Bloom0.BuildMipmaps();

						//filter.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, 2, 0 );
						//filter.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, 2, 1 );
						//filter.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, 2, 2 );
						//filter.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, 2, 3 );

						blur.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, 0 );
						blur.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, 1 );
						blur.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, 2 );
						blur.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, 3 );
					}

					rs.SceneRenderer.RenderForwardTransparent( gameTime, stereoEye, Camera, viewHdrFrame, rlMainView, InstanceGroup.All );
					rs.SceneRenderer.GatherVTFeedbackAndUpdate( gameTime, viewHdrFrame );

					ParticleSystem.RenderSoft( gameTime, Camera, stereoEye, viewHdrFrame );
				}


				using ( new PixEvent( "Frame Postprocessing" ) ) 
				{
					//	compose, tonemap, bloom and color grade :
					rs.HdrFilter.ComposeHdrImage( viewHdrFrame );

					rs.DofFilter.RenderDof( Camera, viewHdrFrame );

					rs.HdrFilter.TonemapHdrImage( gameTime, viewHdrFrame, Camera );

					if (rs.GameFX.Apply( gameTime, viewHdrFrame.TempColor, viewHdrFrame.FinalColor )) {
						viewHdrFrame.SwapFinalColor();
					}

					//	apply FXAA
					if (rs.UseFXAA) {
						rs.Filter.Fxaa( targetSurface, viewHdrFrame.FinalColor );
					} else {
						rs.Filter.Copy( targetSurface, viewHdrFrame.FinalColor );
					} 
				}

				//	draw debug lines :
				Debug.Render( targetSurface, viewHdrFrame.DepthBuffer.Surface, Camera );

				//	draw debug images
				DrawDebugImages( viewport, targetSurface );
			}
		}



		void DrawDebugImages( Viewport viewport, RenderTargetSurface targetSurface )
		{
			switch (rs.ShowGBuffer) {
				case 1  : rs.Filter.CopyColor( targetSurface,	viewHdrFrame.Normals ); return;
				case 2  : rs.Filter.StretchRect( targetSurface,	rs.RayTracer.raytracedImage, new Rectangle(1280-800-64,720-600-64,800,600) ); break;
				case 3  : rs.Filter.CopyColor( targetSurface,	viewHdrFrame.DofCOC ); return;
				case 4  : rs.Filter.CopyColor( targetSurface,	viewHdrFrame.DofForeground ); return;
				case 5  : rs.Filter.CopyColor( targetSurface,	viewHdrFrame.HdrTarget ); return;
				case 6  : rs.Filter.Copy( targetSurface,		viewHdrFrame.AOBuffer ); return;
				case 7  : rs.Filter.StretchRect( targetSurface, rs.LightManager.ShadowMap.ParticleShadowTexture ); return;
				case 8  : rs.Filter.StretchRect( targetSurface, rs.LightManager.ShadowMap.ShadowTexture ); return;
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
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public void BuildRadiosityFormFactor ( string mapName, RadiositySettings settings )
		{
			var lightmapper		=	new LightMapper( rs, settings, Instances );

			var sw				=	new Stopwatch();
			var device			=	Game.GraphicsDevice;
			var builder			=	Game.GetService<Builder>();
			var basePath		=	builder.GetBaseInputDirectory();

			var pathIrrMap		=	Path.Combine(basePath, RenderSystem.LightmapPath, Path.ChangeExtension( mapName, ".irrmap"	) );

			var irrMap = lightmapper.BakeLightMap();

			using ( var stream = File.OpenWrite( pathIrrMap ) ) 
			{
				irrMap.WriteStream( stream );
			}

		}



		void BuildVisibility ()
		{
		}
	}
}
