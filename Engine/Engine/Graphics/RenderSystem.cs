﻿using System;
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

namespace Fusion.Engine.Graphics {

	public partial class RenderSystem : GameComponent {

		internal readonly GraphicsDevice Device;

		internal SpriteEngine	SpriteEngine { get { return spriteEngine; } }
		SpriteEngine	spriteEngine;

		internal Filter Filter { get{ return filter; } }
		Filter filter;

		internal Filter2 Filter2 { get{ return filter2; } }
		Filter2 filter2;

		internal BilateralFilter BilateralFilter { get { return bilateralFilter; } }
		BilateralFilter bilateralFilter;

		internal SsaoFilter SsaoFilter { get{ return ssaoFilter; } }
		SsaoFilter ssaoFilter;

		internal BitonicSort BitonicSort { get{ return bitonicSort; } }
		BitonicSort bitonicSort;

		internal HdrFilter HdrFilter { get{ return hdrFilter; } }
		HdrFilter hdrFilter;
		
		internal DofFilter DofFilter { get{ return dofFilter; } }
		DofFilter dofFilter;
		
		internal LightManager	LightManager { get { return lightManager; } }
		LightManager	lightManager;
		
		internal SceneRenderer	SceneRenderer { get { return sceneRenderer; } }
		SceneRenderer	sceneRenderer;
		
		internal VTSystem	VTSystem { get { return vtSystem; } }
		VTSystem	vtSystem;
		
		internal Sky	Sky { get { return sky; } }
		Sky	sky;

		internal Fog	Fog { get { return fog; } }
		Fog	fog;

		/// <summary>
		/// Gets render counters.
		/// </summary>
		internal RenderCounters Counters { get; private set; }




		RenderTarget2D	hdrTarget;

		public Texture	GrayTexture { get { return grayTexture; } }
		public Texture	WhiteTexture { get { return whiteTexture; } }
		public Texture	BlackTexture { get { return blackTexture; } }
		public Texture	FlatNormalMap { get { return flatNormalMap; } }

		DynamicTexture grayTexture;
		DynamicTexture whiteTexture;
		DynamicTexture blackTexture;
		DynamicTexture flatNormalMap;


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


		/// <summary>
		/// 
		/// </summary>
		/// <param name="engine"></param>
		public RenderSystem ( Game Game ) : base(Game)
		{
			Counters	=	new RenderCounters();

			Width			=	1024;
			Height			=	768;
			Fullscreen		=	false;
			StereoMode		=	StereoMode.Disabled;
			InterlacingMode	=	InterlacingMode.HorizontalLR;
			UseDebugDevice	=	false;
			VSyncInterval	=	1;
			MsaaEnabled		=	false;
			UseFXAA			=	true;

			this.Device	=	Game.GraphicsDevice;

			spriteEngine	=	new SpriteEngine( this );
			filter			=	new Filter( this );
			filter2			=	new Filter2( this );
			bilateralFilter	=	new BilateralFilter( this );
			ssaoFilter		=	new SsaoFilter( this );
			hdrFilter		=	new HdrFilter( this );
			dofFilter		=	new DofFilter( this );
			lightManager	=	new LightManager( this );
			sceneRenderer	=	new SceneRenderer( this );
			sky				=	new Sky( this );
			fog				=	new Fog( this );
			bitonicSort		=	new BitonicSort( this );
			vtSystem	=	new VTSystem( this );

			Game.Config.ExposeConfig( lightManager, "LightRenderer"	, "light" );
			Game.Config.ExposeConfig( ssaoFilter,   "SSAO"			, "ssao"  );
			Game.Config.ExposeConfig( vtSystem,		"VirtualTexture", "vt"	  );
			Game.Invoker.AddCommands( this );

			Device.DisplayBoundsChanged += (s,e) => {
				DisplayBoundsChanged?.Invoke( s, e );
			};
		}


		[Command("screenshot")]
		public string Screenshot_f ( string[] args )
		{
			Screenshot(null);
			return null;
		}


		[Command("vtrestart")]
		public string VTRestart_f ( string[] args )
		{
			Game.RenderSystem.RenderWorld.VirtualTexture = null;
			Game.RenderSystem.RenderWorld.VirtualTexture = Game.Content.Load<VirtualTexture>("*megatexture");
			return null;
		}

		[Command("buildrad")]
		public string CaptureRadiance_f ( string[] args )
		{
			renderWorld?.CaptureRadiance();
			return null;
		}



										  
		/// <summary>
		/// Applies graphics parameters.
		/// </summary>
		/// <param name="p"></param>
		internal void ApplyParameters ( ref GraphicsParameters p )
		{
			p.Width				=	Width;
			p.Height			=	Height;
			p.FullScreen		=	Fullscreen;
			p.StereoMode		=	(Fusion.Drivers.Graphics.StereoMode)StereoMode;
			p.InterlacingMode	=	(Fusion.Drivers.Graphics.InterlacingMode)InterlacingMode;
			p.UseDebugDevice	=	UseDebugDevice;
			p.MsaaLevel			=	1;
		}



		/// <summary>
		/// Intializes graphics engine.
		/// </summary>
		public override void Initialize ()
		{
			//	init components :
			InitializeComponent( spriteEngine	);
			InitializeComponent( filter			);
			InitializeComponent( filter2		);
			InitializeComponent( bilateralFilter);
			InitializeComponent( ssaoFilter		);
			InitializeComponent( hdrFilter		);
			InitializeComponent( dofFilter		);
			InitializeComponent( lightManager	);
			InitializeComponent( sceneRenderer	);
			InitializeComponent( sky			);	
			InitializeComponent( fog			);	
			InitializeComponent( bitonicSort	);
			InitializeComponent( vtSystem );

			//	create default textures :
			whiteTexture	=	new DynamicTexture( this, 4,4, typeof(Color), false, false );
			whiteTexture.SetData( Enumerable.Range(0,16).Select( i => Color.White ).ToArray() );
			
			grayTexture		=	new DynamicTexture( this, 4,4, typeof(Color), false, false );
			grayTexture.SetData( Enumerable.Range(0,16).Select( i => Color.Gray ).ToArray() );

			blackTexture	=	new DynamicTexture( this, 4,4, typeof(Color), false, false );
			blackTexture.SetData( Enumerable.Range(0,16).Select( i => Color.Black ).ToArray() );

			flatNormalMap	=	new DynamicTexture( this, 4,4, typeof(Color), false, false );
			flatNormalMap.SetData( Enumerable.Range(0,16).Select( i => new Color(127,127,255,127) ).ToArray() );

			//	set sprite layers :
			SpriteLayers	=	new SpriteLayerCollection();

			//	add default render world :
			renderWorld		=	new RenderWorld(Game, Width, Height);

			DisplayBoundsChanged += (s,e) => renderWorld.Resize( DisplayBounds.Width, DisplayBounds.Height );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {

				DisposeComponents();

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
		internal void Draw ( GameTime gameTime, StereoEye stereoEye )
		{
			Counters.Reset();

			var targetColorSurface	=	Device.Display.BackbufferColor.Surface;
			var targetDepthSurface	=	Device.Display.BackbufferDepth.Surface;

			//	render world :
			RenderWorld.Render( gameTime, stereoEye, targetColorSurface );

			//	compose rendered image and sprites :
			#warning compose rendered image and sprites :

			//	draw sprites :
			SpriteEngine.DrawSprites( gameTime, stereoEye, targetColorSurface, SpriteLayers );

			if (ShowCounters) {
				Counters.PrintCounters();
			}
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
