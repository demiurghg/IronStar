using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using Fusion.Core.Configuration;
using Fusion.Engine.Graphics.Ubershaders;


namespace Fusion.Engine.Graphics {

	/// <summary>
	/// Represents particle rendering and simulation system.
	/// 1. http://www.gdcvault.com/play/1014347/HALO-REACH-Effects
	/// 2. Gareth Thomas Compute-based GPU Particle
	/// </summary>
	[RequireShader("particles", true)]
	[ShaderSharedStructure(typeof(Particle), typeof(SceneRenderer.LIGHT), typeof(SceneRenderer.LIGHTINDEX))]
	public class ParticleSystem : DisposableBase {

		readonly Game Game;
		readonly RenderSystem rs;
		Ubershader		shader;
		StateFactory	factory;
		RenderWorld	renderWorld;

		[ShaderDefine]	public const int  BLOCK_SIZE			=	256;
		[ShaderDefine]	public const int  MAX_INJECTED			=	4096;
		[ShaderDefine]	public const int  MAX_PARTICLES			=	256 * 256;
		[ShaderDefine]	public const int  MAX_IMAGES			=	512;
		[ShaderDefine]	public const uint ParticleFX_Beam		=	(uint)ParticleFX.Beam		;
		[ShaderDefine]	public const uint ParticleFX_Lit		=	(uint)ParticleFX.Lit			;
		[ShaderDefine]	public const uint ParticleFX_LitShadow	=	(uint)ParticleFX.LitShadow	;
		[ShaderDefine]	public const uint ParticleFX_Shadow		=	(uint)ParticleFX.Shadow		;
		[ShaderDefine]	public const uint LightmapRegionSize	=	1024;
		[ShaderDefine]	public const uint LightmapWidth			=	LightmapRegionSize * 4;
		[ShaderDefine]	public const uint LightmapHeight		=	LightmapRegionSize * 2;

		[ShaderDefine]	public const uint LightTypeOmni			=	SceneRenderer.LightTypeOmni;
		[ShaderDefine]	public const uint LightTypeSpotShadow	=	SceneRenderer.LightTypeSpotShadow;
		[ShaderDefine]	public const uint LightSpotShapeRound	=	SceneRenderer.LightSpotShapeRound;
		[ShaderDefine]	public const uint LightSpotShapeSquare	=	SceneRenderer.LightSpotShapeSquare;

		bool toMuchInjectedParticles = false;

		int					injectionCount = 0;
		Particle[]			injectionBufferCPU = new Particle[MAX_INJECTED];
		StructuredBuffer	injectionBuffer;
		StructuredBuffer	simulationBuffer;
		StructuredBuffer	deadParticlesIndices;
		StructuredBuffer	sortParticlesBuffer;
		StructuredBuffer	particleLighting;
		ConstantBuffer		paramsCB;
		ConstantBuffer		imagesCB;
		RenderTarget2D		lightmap;

		[Config]
		public float SimulationStepTime {
			get { 
				return simulationStepTime; 
			}
			set { 
				if (value<=0) {
					throw new ArgumentOutOfRangeException("value must be positive");
				}
				simulationStepTime = value; 
			}
		}
		float simulationStepTime = 1 / 60.0f;


		/// <summary>
		/// Gets particle lightmap
		/// </summary>
		internal RenderTarget2D Lightmap {
			get { return lightmap; }
		}

		/// <summary>
		/// Gets structured buffer of simulated particles.
		/// </summary>
		internal StructuredBuffer SimulatedParticles {
			get {
				return simulationBuffer;
			}
		}

		/// <summary>
		/// Gets structured buffer of simulated particles.
		/// </summary>
		internal StructuredBuffer ParticleLighting {
			get {
				return particleLighting;
			}
		}

		enum Flags {
			INJECTION		=	0x01,
			SIMULATION		=	0x02,
			DRAW			=	0x04,
			INITIALIZE		=	0x08,
			DRAW_SHADOW		=	0x10,
			DRAW_LIGHT		=	0x20,
			ALLOC_LIGHTMAP	=	0x40,
		}


//       row_major float4x4 View;       // Offset:    0
//       row_major float4x4 Projection; // Offset:   64
//       float4 CameraForward;          // Offset:  128
//       float4 CameraRight;            // Offset:  144
//       float4 CameraUp;               // Offset:  160
//       float4 CameraPosition;         // Offset:  176
//       float4 Gravity;                // Offset:  192
//       float LinearizeDepthA;         // Offset:  208
//       float LinearizeDepthB;         // Offset:  212
//       int MaxParticles;              // Offset:  216
//       float DeltaTime;               // Offset:  220
//       uint DeadListSize;             // Offset:  224
		[StructLayout(LayoutKind.Sequential, Size=1024)]
		[ShaderStructure]
		struct PARAMS {
			public Matrix	View;
			public Matrix	Projection;
			public Matrix	ViewProjection;
			public Matrix	CascadeViewProjection0	;
			public Matrix	CascadeViewProjection1	;
			public Matrix	CascadeViewProjection2	;
			public Matrix	CascadeViewProjection3	;
			public Vector4	CascadeScaleOffset0		;
			public Vector4	CascadeScaleOffset1		;
			public Vector4	CascadeScaleOffset2		;
			public Vector4	CascadeScaleOffset3		;
			public Vector4	CameraForward;
			public Vector4	CameraRight;
			public Vector4	CameraUp;
			public Vector4	CameraPosition;
			public Vector4	Gravity;
			public Vector4	LightMapSize;
			public Vector4	DirectLightDirection;
			public Color4	DirectLightIntensity;
			public float	LinearizeDepthA;
			public float	LinearizeDepthB;
			public int		MaxParticles;
			public float	DeltaTime;
			public uint		DeadListSize;
			public float	CocScale;
			public float	CocBias;
		} 

		Random rand = new Random();


		/// <summary>
		/// Gets and sets overall particle gravity.
		/// Default -9.8.
		/// </summary>
		public Vector3	Gravity { get; set; }
		


		/// <summary>
		/// Sets and gets images for particles.
		/// This property must be set before particle injection.
		/// To prevent interference between textures in atlas all images must be padded with 16 pixels.
		/// </summary>
		public TextureAtlas Images { 
			get {
				return images;
			}
			set {
				if (value!=null && value.Count>MAX_IMAGES) {
					throw new ArgumentOutOfRangeException("Number of subimages in texture atlas is greater than " + MAX_IMAGES.ToString() );
				}
				images = value;
			}
		}

		TextureAtlas images = null;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		internal ParticleSystem ( RenderSystem rs, RenderWorld renderWorld )
		{
			this.rs				=	rs;
			this.Game			=	rs.Game;
			this.renderWorld	=	renderWorld;

			Gravity	=	Vector3.Down * 9.80665f;

			paramsCB		=	new ConstantBuffer( Game.GraphicsDevice, typeof(PARAMS) );
			imagesCB		=	new ConstantBuffer( Game.GraphicsDevice, typeof(Vector4), MAX_IMAGES );

			injectionBuffer			=	new StructuredBuffer( Game.GraphicsDevice, typeof(Particle),	MAX_INJECTED, StructuredBufferFlags.None );
			simulationBuffer		=	new StructuredBuffer( Game.GraphicsDevice, typeof(Particle),	MAX_PARTICLES, StructuredBufferFlags.None );
			particleLighting		=	new StructuredBuffer( Game.GraphicsDevice, typeof(Vector4),		MAX_PARTICLES, StructuredBufferFlags.None );
			sortParticlesBuffer		=	new StructuredBuffer( Game.GraphicsDevice, typeof(Vector2),		MAX_PARTICLES, StructuredBufferFlags.None );
			deadParticlesIndices	=	new StructuredBuffer( Game.GraphicsDevice, typeof(uint),		MAX_PARTICLES, StructuredBufferFlags.Append );

			lightmap				=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.Rgba16F,	(int)LightmapWidth, (int)LightmapHeight, false );

			rs.Game.Reloading += LoadContent;
			LoadContent(this, EventArgs.Empty);

			//	initialize dead list :
			var device = Game.GraphicsDevice;

			device.SetCSRWBuffer( 1, deadParticlesIndices, 0 );
			device.PipelineState	=	factory[ (int)Flags.INITIALIZE ];
			device.Dispatch( MathUtil.IntDivUp( MAX_PARTICLES, BLOCK_SIZE ) );
		}



		/// <summary>
		/// Loads content
		/// </summary>
		void LoadContent ( object sender, EventArgs args )
		{
			shader	=	Game.Content.Load<Ubershader>("particles");
			factory	=	shader.CreateFactory( typeof(Flags), (ps,i) => EnumAction( ps, (Flags)i ) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {	

				rs.Game.Reloading -= LoadContent;

				SafeDispose( ref paramsCB );
				SafeDispose( ref imagesCB );

				SafeDispose( ref lightmap );

				SafeDispose( ref injectionBuffer );
				SafeDispose( ref simulationBuffer );
				SafeDispose( ref particleLighting );
				SafeDispose( ref sortParticlesBuffer );
				SafeDispose( ref deadParticlesIndices );
			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="ps"></param>
		/// <param name="flag"></param>
		void EnumAction ( PipelineState ps, Flags flag )
		{
			if (flag==Flags.DRAW) {
				ps.BlendState			=	BlendState.AlphaBlend;
				ps.DepthStencilState	=	DepthStencilState.None;
				ps.Primitive			=	Primitive.PointList;
				ps.RasterizerState		=	RasterizerState.CullNone;
			}

			if (flag==Flags.DRAW_SHADOW) {

				var bs = new BlendState();
				bs.DstAlpha	=	Blend.One;
				bs.SrcAlpha	=	Blend.One;
				bs.SrcColor	=	Blend.DstColor;
				bs.DstColor	=	Blend.Zero;
				bs.AlphaOp	=	BlendOp.Add;

				ps.BlendState			=	bs;
				ps.DepthStencilState	=	DepthStencilState.Readonly;
				ps.Primitive			=	Primitive.PointList;
				ps.RasterizerState		=	RasterizerState.CullNone;
			}

			if (flag==Flags.DRAW_LIGHT) {
				ps.BlendState			=	BlendState.Opaque;
				ps.DepthStencilState	=	DepthStencilState.None;
				ps.Primitive			=	Primitive.PointList;
				ps.RasterizerState		=	RasterizerState.CullNone;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public TextureAtlas TextureAtlas {
			get; set;
		}


		/// <summary>
		/// Injects hard particle.
		/// </summary>
		/// <param name="particle"></param>
		public void InjectParticle ( Particle particle )
		{
			if (renderWorld.IsPaused) {
				return;
			}

			if (Images==null) {
				throw new InvalidOperationException("Images must be set");
			}

			if (injectionCount>=MAX_INJECTED) {
				toMuchInjectedParticles = true;
				return;
			}

			if (particle.LifeTime<=0) {
				return;
			}

			toMuchInjectedParticles = false;

			injectionBufferCPU[ injectionCount ] = particle;
			injectionCount ++;
		}



		/// <summary>
		/// Makes all particles wittingly dead
		/// </summary>
		void ClearParticleBuffer ()
		{
			injectionCount = 0;
		}



		/// <summary>
		/// Immediatly kills all living particles.
		/// </summary>
		/// <returns></returns>
		public void KillParticles ()
		{
			timeAccumulator	=	0;
			requestKill = true;
			ClearParticleBuffer();
		}

		bool requestKill = false;



		/// <summary>
		/// 
		/// </summary>
		void SetupGPUParameters ( float stepTime, RenderWorld renderWorld, Matrix view, Matrix projection, Flags flags )
		{
			var deltaTime		=	stepTime;
			var camera			=	renderWorld.Camera;
			var cameraMatrix	=	Matrix.Invert( view );

			//	kill particles by applying very large delta.
			if (requestKill) {
				deltaTime	=	float.MaxValue / 2;
				requestKill	=	false;
			}
			if (rs.FreezeParticles) {
				deltaTime = 0;
			}

			//	fill constant data :
			PARAMS param		=	new PARAMS();

			param.View				=	view;
			param.Projection        =   projection;
			param.ViewProjection	=	view * projection;
			param.CascadeViewProjection0	=	rs.LightManager.ShadowMap.GetCascade( 0 ).ViewProjectionMatrix;
			param.CascadeViewProjection1	=	rs.LightManager.ShadowMap.GetCascade( 1 ).ViewProjectionMatrix;
			param.CascadeViewProjection2	=	rs.LightManager.ShadowMap.GetCascade( 2 ).ViewProjectionMatrix;
			param.CascadeViewProjection3	=	rs.LightManager.ShadowMap.GetCascade( 3 ).ViewProjectionMatrix;
			param.CascadeScaleOffset0		=	rs.LightManager.ShadowMap.GetCascade( 0 ).ShadowScaleOffset;
			param.CascadeScaleOffset1		=	rs.LightManager.ShadowMap.GetCascade( 1 ).ShadowScaleOffset;
			param.CascadeScaleOffset2		=	rs.LightManager.ShadowMap.GetCascade( 2 ).ShadowScaleOffset;
			param.CascadeScaleOffset3		=	rs.LightManager.ShadowMap.GetCascade( 3 ).ShadowScaleOffset;
			param.DirectLightDirection		=	new Vector4( renderWorld.LightSet.DirectLight.Direction, 0 );
			param.DirectLightIntensity		=	renderWorld.LightSet.DirectLight.Intensity;
			param.MaxParticles		=	0;
			param.DeltaTime			=	deltaTime;
			param.CameraForward		=	new Vector4( cameraMatrix.Forward	, 0 );
			param.CameraRight		=	new Vector4( cameraMatrix.Right	, 0 );
			param.CameraUp			=	new Vector4( cameraMatrix.Up		, 0 );
			param.CameraPosition	=	new Vector4( cameraMatrix.TranslationVector	, 1 );
			param.Gravity			=	new Vector4( this.Gravity, 0 );
			param.LightMapSize		=	new Vector4( Lightmap.Width, Lightmap.Height, 1.0f/Lightmap.Width, 1.0f/Lightmap.Height );
			param.MaxParticles		=	MAX_PARTICLES;
			param.LinearizeDepthA	=	camera.LinearizeDepthScale;
			param.LinearizeDepthB	=	camera.LinearizeDepthBias;
			param.CocBias			=	renderWorld.DofSettings.CocBias;
			param.CocScale			=	renderWorld.DofSettings.CocScale;

			if (flags==Flags.INJECTION) {
				param.MaxParticles	=	injectionCount;
			}

			//	copy to gpu :
			paramsCB.SetData( param );

			//	set DeadListSize to prevent underflow:
			if (flags==Flags.INJECTION) {
				deadParticlesIndices.CopyStructureCount( paramsCB, Marshal.OffsetOf( typeof(PARAMS), "DeadListSize").ToInt32() );
			}
		}



		float timeAccumulator = 0;



		/// <summary>
		/// Updates particle properties.
		/// </summary>
		/// <param name="gameTime"></param>
		internal void Simulate ( GameTime gameTime, Camera camera )
		{
			var device	=	Game.GraphicsDevice;

			var view		=	camera.GetViewMatrix( StereoEye.Mono );
			var projection	=	camera.GetProjectionMatrix( StereoEye.Mono );


			timeAccumulator	+=	gameTime.ElapsedSec;


			using ( new PixEvent("Particle Simulation") ) {

				device.ResetStates();

				//
				//	Inject :
				//
				using (new PixEvent("Injection")) {

					injectionBuffer.SetData( injectionBufferCPU );

					device.ComputeShaderResources[1]	= injectionBuffer ;
					device.SetCSRWBuffer( 0, simulationBuffer,		0 );
					device.SetCSRWBuffer( 1, deadParticlesIndices, -1 );

					SetupGPUParameters( 0, renderWorld, view, projection, Flags.INJECTION );
					device.ComputeShaderConstants[0]	= paramsCB ;

					device.PipelineState	=	factory[ (int)Flags.INJECTION ];
			
					//	GPU time ???? -> 0.0046
					device.Dispatch( MathUtil.IntDivUp( MAX_INJECTED, BLOCK_SIZE ) );

					ClearParticleBuffer();
				}


				//
				//	Simulate :
				//
				using (new PixEvent("Simulation")) {

					if (!renderWorld.IsPaused && !rs.SkipParticlesSimulation) {
	
						device.SetCSRWBuffer( 0, simulationBuffer,		0 );
						device.SetCSRWBuffer( 1, deadParticlesIndices, -1 );
						device.SetCSRWBuffer( 2, sortParticlesBuffer, 0 );

						float stepTime = SimulationStepTime;

						while ( timeAccumulator > stepTime ) {

							SetupGPUParameters( stepTime, renderWorld, view, projection, Flags.SIMULATION);
							device.ComputeShaderConstants[0] = paramsCB ;

							device.PipelineState	=	factory[ (int)Flags.SIMULATION ];
	
							/// GPU time : 1.665 ms	 --> 0.38 ms
							device.Dispatch( MathUtil.IntDivUp( MAX_PARTICLES, BLOCK_SIZE ) );//*/

							timeAccumulator -= stepTime;
						}
					}
				}


				//
				//	Alloc Lightmap :
				//
				using (new PixEvent("Alloc LightMap")) {

					if (!renderWorld.IsPaused && !rs.SkipParticlesSimulation) {
	
						device.SetCSRWBuffer( 0, simulationBuffer,		0 );
						device.SetCSRWBuffer( 1, deadParticlesIndices, -1 );
						device.SetCSRWBuffer( 2, sortParticlesBuffer, 0 );

						SetupGPUParameters( 0, renderWorld, view, projection, Flags.ALLOC_LIGHTMAP );
						device.ComputeShaderConstants[0] = paramsCB ;

						device.PipelineState	=	factory[ (int)Flags.ALLOC_LIGHTMAP ];
	
						device.Dispatch( 1, 1, 1 );//*/
					}
				}


				//
				//	Sort :
				//
				using ( new PixEvent( "Sort" ) ) {
					rs.BitonicSort.Sort( sortParticlesBuffer );
				}


				if (rs.ShowParticles) {
					rs.Counters.DeadParticles	=	deadParticlesIndices.GetStructureCount();
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		void RenderGeneric ( string passName, GameTime gameTime, Camera camera, Viewport viewport, Matrix view, Matrix projection, RenderTargetSurface colorTarget, DepthStencilSurface depthTarget, ShaderResource depthValues, Flags flags )
		{
			var device	=	Game.GraphicsDevice;

			using ( new PixEvent(passName) ) {

				device.ResetStates();

				//
				//	Setup images :
				//
				if (Images!=null && !Images.IsDisposed) {
					imagesCB.SetData( Images.GetNormalizedRectangles( MAX_IMAGES ) );
				}

				SetupGPUParameters( 0, renderWorld, view, projection, flags );
				device.ComputeShaderConstants[0] = paramsCB ;

				//
				//	Render
				//
				using (new PixEvent("Drawing")) {

	
					//	target and viewport :
					device.SetTargets( depthTarget, colorTarget );
					device.SetViewport( viewport );

					//	params CB :			
					device.ComputeShaderConstants[0]	= paramsCB ;
					device.VertexShaderConstants[0]		= paramsCB ;
					device.GeometryShaderConstants[0]	= paramsCB ;
					device.PixelShaderConstants[0]		= paramsCB ;

					//	atlas CB :
					device.VertexShaderConstants[1]		= imagesCB ;
					device.GeometryShaderConstants[1]	= imagesCB ;
					device.PixelShaderConstants[1]		= imagesCB ;

					//	sampler & textures :
					device.PixelShaderSamplers[0]		=	SamplerState.LinearClamp4Mips ;

					device.PixelShaderResources[0]		=	Images==null? rs.WhiteTexture.Srv : Images.Texture.Srv;
					device.PixelShaderResources[5]		=	depthValues;
					device.GeometryShaderResources[1]	=	simulationBuffer ;
					device.GeometryShaderResources[2]	=	simulationBuffer ;
					device.GeometryShaderResources[3]	=	sortParticlesBuffer;
					device.GeometryShaderResources[4]	=	particleLighting;

					if (flags==Flags.DRAW_LIGHT) {
						device.PixelShaderResources[7]		=	rs.LightManager.LightGrid.GridTexture;
						device.PixelShaderResources[8]		=	rs.LightManager.LightGrid.IndexDataGpu;
						device.PixelShaderResources[9]		=	rs.LightManager.LightGrid.LightDataGpu;
						device.PixelShaderResources[10]		=	rs.LightManager.ShadowMap.ColorBuffer;

						device.PixelShaderSamplers[1]		=	SamplerState.ShadowSampler ;
					}

					if (flags==Flags.DRAW) {
						device.PixelShaderResources[11]		=	Lightmap;
					}

					//	setup PS :
					device.PipelineState	=	factory[ (int)flags ];

					//	GPU time : 0.81 ms	-> 0.91 ms
					device.Draw( MAX_PARTICLES, 0 );
				}
			}
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		internal void Render ( GameTime gameTime, Camera camera, StereoEye stereoEye, HdrFrame viewFrame )
		{
			if (rs.SkipParticles) {
				return;
			}

			var view		=	camera.GetViewMatrix( stereoEye );
			var projection	=	camera.GetProjectionMatrix( stereoEye );

			var colorTarget	=	viewFrame.HdrBuffer.Surface;
			var depthTarget	=	viewFrame.DepthBuffer.Surface;

			var viewport	=	new Viewport( 0, 0, colorTarget.Width, colorTarget.Height );

			RenderGeneric( "Particles", gameTime, camera, viewport, view, projection, colorTarget, null, viewFrame.DepthBuffer, Flags.DRAW );
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		internal void RenderLight ( GameTime gameTime, Camera camera )
		{
			if (rs.SkipParticles) {
				return;
			}

			rs.Device.Clear( lightmap.Surface, Color4.Black );

			var view		=	camera.GetViewMatrix( StereoEye.Mono );
			var projection	=	camera.GetProjectionMatrix( StereoEye.Mono );

			var colorTarget	=	lightmap.Surface;

			var viewport	=	new Viewport( 0, 0, lightmap.Width, lightmap.Height );

			RenderGeneric( "Particles Light", gameTime, camera, viewport, view, projection, colorTarget, null, null, Flags.DRAW_LIGHT );
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		internal void RenderShadow ( GameTime gameTime, Viewport viewport, Matrix view, Matrix projection, RenderTargetSurface particleShadow, DepthStencilSurface depthBuffer )
		{
			if (rs.SkipParticleShadows) {
				return;
			}

			var colorTarget	=	particleShadow;
			var depthTarget	=	depthBuffer;

			RenderGeneric( "Particles Shadow", gameTime, null, viewport, view, projection, colorTarget, depthTarget, null, Flags.DRAW_SHADOW );
		}
	}
}
