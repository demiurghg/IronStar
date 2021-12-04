using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using Fusion.Core.Shell;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics.Lights;
using Fusion.Engine.Imaging;
using BEPUphysics.Paths;
using System.IO;
using Fusion.Engine.Graphics.GI;
using Fusion.Engine.Graphics.GUI;

namespace Fusion.Engine.Graphics {

	public partial class RenderSystem : GameComponent, IRenderSystem 
	{
		internal readonly GraphicsDevice Device;

		readonly RenderStats stats = new RenderStats();
		public RenderStats Stats { get { return stats; } }


		internal SpriteEngine			SpriteEngine		{ get { return Game.GetService< SpriteEngine		>(); } }
		internal Filter					Filter				{ get { return Game.GetService< Filter				>(); } }
		internal Filter2				Filter2				{ get { return Game.GetService< Filter2				>(); } }
		internal BlurFilter				Blur				{ get { return Game.GetService< BlurFilter			>(); } }
		internal BilateralFilter		BilateralFilter		{ get { return Game.GetService< BilateralFilter		>(); } }
		internal DilateFilter			DilateFilter		{ get { return Game.GetService< DilateFilter		>(); } }
		internal SsaoFilter				SsaoFilter			{ get { return Game.GetService< SsaoFilter			>(); } }
		internal BitonicSort			BitonicSort			{ get { return Game.GetService< BitonicSort			>(); } }
		internal HdrFilter				HdrFilter			{ get { return Game.GetService< HdrFilter			>(); } }
		public   DofFilter				DofFilter			{ get { return Game.GetService< DofFilter			>(); } }
		internal LightManager			LightManager		{ get { return Game.GetService< LightManager		>(); } }
		internal LightProbeRelighter	LightProbeRelighter	{ get { return Game.GetService< LightProbeRelighter	>(); } }
		internal SceneRenderer			SceneRenderer		{ get { return Game.GetService< SceneRenderer		>(); } }
		internal VTSystem				VTSystem			{ get { return Game.GetService< VTSystem			>(); } }
		public   Sky2					Sky					{ get { return Game.GetService< Sky2				>(); } }
		internal Fog					Fog					{ get { return Game.GetService< Fog					>(); } }
		internal LightProbeViewer		LightMapDebugger	{ get { return Game.GetService< LightProbeViewer		>(); } }
		public Radiosity				Radiosity			{ get { return Game.GetService< Radiosity			>(); } }
		public RayTracer				RayTracer			{ get { return Game.GetService< RayTracer			>(); } }
		public GameFX					GameFX				{ get { return Game.GetService< GameFX				>(); } }
		public LightProbeBaker			LightProbeBaker		{ get { return Game.GetService< LightProbeBaker		>(); } }
		internal ShadowSystem			ShadowSystem		{ get { return Game.GetService< ShadowSystem		>(); } }
		public GuiRenderer				GuiRenderer			{ get { return Game.GetService< GuiRenderer			>(); } }

		/// <summary>
		/// Gets render counters.
		/// </summary>
		public Texture	GrayTexture { get { return grayTexture; } }
		public Texture	WhiteTexture { get { return whiteTexture; } }
		public Texture	BlackTexture { get { return blackTexture; } }
		public Texture	FlatNormalMap { get { return flatNormalMap; } }

		public RenderTarget2D	PreviewTarget { get { return previewTarget; } }

		public LightMapResources LightMapResources { get { return lightMapResources; } }
		LightMapResources lightMapResources;

		DynamicTexture grayTexture;
		DynamicTexture whiteTexture;
		DynamicTexture blackTexture;
		DynamicTexture flatNormalMap;
		RenderTarget2D previewTarget;

		internal SpriteLayer	extentTest;


		/// <summary>
		/// Gets render world.
		/// </summary>
		public RenderWorld RenderWorld {
			get {
				return renderWorld;
			}
		}

		RenderWorld renderWorld;


		/// <summary>
		/// Gets collection of sprite layers.
		/// </summary>
		public ICollection<SpriteLayer>	SpriteLayers {
			get; private set;
		}


		readonly bool useRenderWorld;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="engine"></param>
		public RenderSystem ( Game Game, bool useRenderWorld ) : base(Game)
		{
			this.useRenderWorld	=	useRenderWorld;

			Width			=	1024;
			Height			=	768;
			Fullscreen		=	false;
			StereoMode		=	StereoMode.Disabled;
			InterlacingMode	=	InterlacingMode.HorizontalLR;
			UseDebugDevice	=	false;
			VSyncInterval	=	1;
			UseFXAA			=	true;

			this.Device	=	Game.GraphicsDevice;

			Game.AddServiceAndComponent( new SpriteEngine		( this ) );
			Game.AddServiceAndComponent( new Filter				( this ) );
			Game.AddServiceAndComponent( new Filter2			( this ) );
			Game.AddServiceAndComponent( new CubeMapFilter		( this ) );
			Game.AddServiceAndComponent( new BlurFilter			( this ) );
			Game.AddServiceAndComponent( new BilateralFilter	( this ) );
			Game.AddServiceAndComponent( new DilateFilter		( this ) );
			Game.AddServiceAndComponent( new SsaoFilter			( this ) );
			Game.AddServiceAndComponent( new DofFilter			( this ) );
			Game.AddServiceAndComponent( new HdrFilter			( this ) );
			Game.AddServiceAndComponent( new GameFX				( this ) );
			Game.AddServiceAndComponent( new LightManager		( this ) );
			Game.AddServiceAndComponent( new LightProbeRelighter( this ) );
			Game.AddServiceAndComponent( new ShadowSystem		( this ) );
			Game.AddServiceAndComponent( new SceneRenderer		( this ) );
			Game.AddServiceAndComponent( new Sky2				( this ) );
			Game.AddServiceAndComponent( new Fog				( this ) );
			Game.AddServiceAndComponent( new BitonicSort		( this ) );
			Game.AddServiceAndComponent( new VTSystem			( this ) );
			Game.AddServiceAndComponent( new LightProbeViewer	( this ) );

			Game.AddServiceAndComponent( new Radiosity			( this ) );
			Game.AddServiceAndComponent( new RayTracer			( this ) );
			Game.AddServiceAndComponent( new LightProbeBaker	( this ) );
			Game.AddServiceAndComponent( new GuiRenderer			( this ) );

			Device.DisplayBoundsChanged += (s,e) => {
				DisplayBoundsChanged?.Invoke( s, e );
			};
		}


		/// <summary>
		/// Applies graphics parameters.
		/// </summary>
		/// <param name="p"></param>
		public void ApplyParameters ( ref GraphicsParameters p )
		{
			p.Width				=	Width;
			p.Height			=	Height;
			p.FullScreen		=	Fullscreen;
			p.StereoMode		=	(Fusion.Drivers.Graphics.StereoMode)StereoMode;
			p.InterlacingMode	=	(Fusion.Drivers.Graphics.InterlacingMode)InterlacingMode;
			p.UseDebugDevice	=	UseDebugDevice;
			p.UseRenderDoc		=	UseRenderDoc;
			p.MsaaLevel			=	1;
		}



		/// <summary>
		/// Intializes graphics engine.
		/// </summary>
		public override void Initialize ()
		{
			RegisterCommands();

			//	create default textures :
			whiteTexture	=	new DynamicTexture( this, 4,4, typeof(Color), false, false );
			whiteTexture.SetData( Enumerable.Range(0,16).Select( i => Color.White ).ToArray() );
			
			grayTexture		=	new DynamicTexture( this, 4,4, typeof(Color), false, false );
			grayTexture.SetData( Enumerable.Range(0,16).Select( i => Color.Gray ).ToArray() );

			blackTexture	=	new DynamicTexture( this, 4,4, typeof(Color), false, false );
			blackTexture.SetData( Enumerable.Range(0,16).Select( i => Color.Black ).ToArray() );

			flatNormalMap	=	new DynamicTexture( this, 4,4, typeof(Color), false, false );
			flatNormalMap.SetData( Enumerable.Range(0,16).Select( i => new Color(127,127,255,127) ).ToArray() );

			previewTarget	=	new RenderTarget2D( this.Device, ColorFormat.Rgba8, PreviewWidth, PreviewHeight, false );

			lightMapResources	=	new LightMapResources(this);

			//	set sprite layers :
			SpriteLayers	=	new SpriteLayerCollection();

			//	add default render world :
			if (useRenderWorld) {
				renderWorld		=	new RenderWorld(Game, Width, Height);
			}

			DisplayBoundsChanged += (s,e) => renderWorld.Resize( DisplayBounds.Width, DisplayBounds.Height );
			Game.Exiting+=Game_Exiting;

			//	add extent layer test :
			extentTest			=	new SpriteLayer(this, 100);
			extentTest.Order	=	0;
			SpriteLayers.Add(extentTest);
		}


		private void Game_Exiting( object sender, EventArgs e )
		{
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) 
			{
				SafeDispose( ref lightMapResources );

				SafeDispose( ref previewTarget );

				SafeDispose( ref renderWorld );
				SafeDispose( ref grayTexture );
				SafeDispose( ref whiteTexture );
				SafeDispose( ref blackTexture );
				SafeDispose( ref flatNormalMap );
			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="stereoEye"></param>
		public void RenderView ( GameTime gameTime, StereoEye stereoEye )
		{
			Stats.Reset();
			Stats.InstanceCount		=	RenderWorld == null ? 0 : RenderWorld.Instances.Count;

			var targetColorSurface	=	Device.Display.BackbufferColor.Surface;
			var targetDepthSurface	=	Device.Display.BackbufferDepth.Surface;

			//	render world :
			RenderWorld?.Render( gameTime, stereoEye, targetColorSurface );

			MakePreviewInternal();

			//	compose rendered image and sprites :
			#warning compose rendered image and sprites :

			//	draw sprites :
			SpriteEngine.DrawSprites( gameTime, stereoEye, targetColorSurface, SpriteLayers );
		}


		public void MakePreviewScreenshot(string path)
		{
			requestPreviewPath = path;
		}

		string requestPreviewPath = null;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		void MakePreviewInternal ()
		{
			if (requestPreviewPath==null)
			{
				return;
			}

			var renderTarget	=	Device.Display.BackbufferColor;
			var path			=	Path.ChangeExtension(requestPreviewPath, ".tga");
			var image			=	new Image<Color>( renderTarget.Width, renderTarget.Height, new Color(255,0,128,255) );

			renderTarget.GetData( image.RawImageData );
			ImageLib.SetAlpha( image, 255 );
			image = ImageLib.Downsample( image, PreviewWidth, PreviewHeight );

			using ( var stream = File.OpenWrite( path ) )
			{
				ImageLib.SaveTga( image, stream );
			}

			requestPreviewPath = null;
		}

		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Display stuff :
		 * 
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		public void Screenshot ( string path = null )
		{
			Device.Screenshot(path);
		}
		

		/// <summary>
		/// Gets display bounds.
		/// </summary>
		public Rectangle DisplayBounds {
			get {
				return Device.DisplayBounds;
			}
		}


		/// <summary>
		/// Raises when display bound changes.
		/// DisplayBounds property is already has actual value when this event raised.
		/// </summary>
		public event EventHandler	DisplayBoundsChanged;
	}
}
