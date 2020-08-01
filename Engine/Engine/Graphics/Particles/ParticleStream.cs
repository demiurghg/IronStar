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
	public class ParticleStream : RenderComponent 
	{
		static FXConstantBuffer<PARAMS>						regParams				=	new CRegister( 0, "Params"					);
		static FXConstantBuffer<GpuData.CAMERA>				regCamera				=	new CRegister( 1, "Camera"					);
		static FXConstantBuffer<GpuData.CAMERA>				regCameraWeapon			=	new CRegister( 2, "CameraWeapon"			);
		static FXConstantBuffer<Vector4>					regImages				=	new CRegister( 3, MAX_IMAGES, "Images"		);
		static FXConstantBuffer<GpuData.DIRECT_LIGHT>		regDirectLight			=	new CRegister( 4, "DirectLight"				);
		static FXConstantBuffer<ShadowMap.CASCADE_SHADOW>	regCascadeShadow		=	new CRegister( 5, "CascadeShadow"			);
		static FXConstantBuffer<Fog.FOG_DATA>				regFog					=	new CRegister( 6, "Fog"						);

		static FXSamplerState								regSampler					=	new SRegister( 0, "LinearSampler"			);
		static FXSamplerComparisonState						regShadowSampler			=	new SRegister( 1, "ShadowSampler"			);
																										   
		static FXTexture2D<Vector4>							regTexture 					=	new TRegister( 0, "Texture" 				);
		static FXStructuredBuffer<Particle>					reginjectionBuffer			=	new TRegister( 1, "injectionBuffer"			);
		static FXStructuredBuffer<Particle>					regparticleBufferGS			=	new TRegister( 2, "particleBufferGS"		);
		static FXStructuredBuffer<Vector2>					regsortParticleBufferGS		=	new TRegister( 3, "sortParticleBufferGS"	);
		static FXTexture2D<Vector4>							regDepthValues				=	new TRegister( 5, "DepthValues"				);
		static FXTexture2D<Vector4>							regColorTemperature			=	new TRegister( 6, "ColorTemperature"		);
																										   	
		static FXTexture3D<UInt2>							regClusterTable				=	new TRegister( 7, "ClusterArray"			);
		static FXBuffer<uint>								regLightIndexTable			=	new TRegister( 8, "ClusterIndexBuffer"			);
		static FXStructuredBuffer<SceneRenderer.LIGHT>		regLightDataTable			=	new TRegister( 9, "ClusterLightBuffer"			);
		static FXTexture2D<Vector4>							regShadowMap				=	new TRegister(10, "ShadowMap"				);
		static FXTexture2D<Vector4>							regLightMap					=	new TRegister(11, "LightMap"				);
		static FXTexture2D<Vector4>							regShadowMask				=	new TRegister(12, "ShadowMask"				);

		static FXTexture3D<Vector4>							regFogVolume				=	new TRegister(13, "FogVolume"				);

		static FXTexture3D<Vector4>							regIrradianceVolumeL0		=	new TRegister(14, "IrradianceVolumeL0"		);
		static FXTexture3D<Vector4>							regIrradianceVolumeL1		=	new TRegister(15, "IrradianceVolumeL1"		);
		static FXTexture3D<Vector4>							regIrradianceVolumeL2		=	new TRegister(16, "IrradianceVolumeL2"		);
		static FXTexture3D<Vector4>							regIrradianceVolumeL3		=	new TRegister(17, "IrradianceVolumeL3"		);
																											
		static FXStructuredBuffer<Vector4>					regLightMapRegionsGS		=	new TRegister(18, "lightMapRegionsGS"		);
																															
		static FXRWStructuredBuffer<Particle>				regparticleBuffer			=	new URegister( 0, "particleBuffer"			);
		static FXConsumeStructuredBuffer<uint>				regdeadParticleIndicesPull	=	new URegister( 1, "deadParticleIndicesPull"	);
		static FXAppendStructuredBuffer<uint>				regdeadParticleIndicesPush	=	new URegister( 1, "deadParticleIndicesPush"	);
		static FXRWStructuredBuffer<Vector2>				regsortParticleBuffer		=	new URegister( 2, "sortParticleBuffer"		);
		static FXRWStructuredBuffer<Vector4>				reglightMapRegions			=	new URegister( 3, "lightMapRegions"			);


		readonly ParticleSystem ps;
		readonly int particleCount;
		readonly bool sortParticles;
		readonly bool useLightmap;
		Ubershader		shader;
		StateFactory	factory;
		RenderWorld	renderWorld;

		[ShaderDefine]	public const int  BLOCK_SIZE				=	256;
		[ShaderDefine]	public const int  MAX_INJECTED				=	4096;
		[ShaderDefine]	public const int  MAX_PARTICLES				=	256 * 256;
		[ShaderDefine]	public const int  MAX_IMAGES				=	512;

		[ShaderDefine]	public const uint ParticleFX_Hard			=	(uint)ParticleFX.Hard			;
		[ShaderDefine]	public const uint ParticleFX_HardLit		=	(uint)ParticleFX.HardLit		;
		[ShaderDefine]	public const uint ParticleFX_HardLitShadow	=	(uint)ParticleFX.HardLitShadow	;
		[ShaderDefine]	public const uint ParticleFX_Soft			=	(uint)ParticleFX.Soft			;
		[ShaderDefine]	public const uint ParticleFX_SoftLit		=	(uint)ParticleFX.SoftLit		;
		[ShaderDefine]	public const uint ParticleFX_SoftLitShadow	=	(uint)ParticleFX.SoftLitShadow	;
		[ShaderDefine]	public const uint ParticleFX_Distortive		=	(uint)ParticleFX.Distortive		;

		[ShaderDefine]	public const uint LightmapRegionSize	=	1024;
		[ShaderDefine]	public const uint LightmapWidth			=	LightmapRegionSize * 4;
		[ShaderDefine]	public const uint LightmapHeight		=	LightmapRegionSize * 2;

		bool toMuchInjectedParticles = false;

		int					injectionCount = 0;
		Particle[]			injectionBufferCPU = new Particle[MAX_INJECTED];
		StructuredBuffer	injectionBuffer;
		StructuredBuffer	simulationBuffer;
		StructuredBuffer	deadParticlesIndices;
		StructuredBuffer	sortParticlesBuffer;
		StructuredBuffer	lightMapRegions;
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
		float simulationStepTime = 1 / 1000.0f;


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

		enum Flags {
			INJECTION		=	0x0001,
			SIMULATION		=	0x0002,
			INITIALIZE		=	0x0004,
			ALLOC_LIGHTMAP	=	0x0008,
			DRAW			=	0x0100,
			SOFT			=	0x0200,
			HARD			=	0x0400,
			DUDV			=	0x0800,
			VELOCITY		=	0x1000,
			SOFT_SHADOW		=	0x2000,
			HARD_SHADOW		=	0x4000,
			LIGHTMAP		=	0x8000,
		}


		[StructLayout(LayoutKind.Sequential, Size=128)]
		[ShaderStructure]
		struct PARAMS {
			public Vector4	WorldToVoxelScale;
			public Vector4	WorldToVoxelOffset;
			public Vector4	Gravity;
			public Vector4	LightMapSize;
			public Color4	SkyAmbientLevel;
			public int		MaxParticles;
			public float	DeltaTime;
			public uint		DeadListSize;
			public float	CocScale;
			public float	CocBias;
			public uint		IntegrationSteps;
			public float	IndirectLightFactor;
			public float	DirectLightFactor;
		} 

		Random rand = new Random();


		/// <summary>
		/// Gets and sets overall particle gravity.
		/// Default -9.8.
		/// </summary>
		Vector3	Gravity { 
			get {
				return ps.Gravity;
			}
		}
		

		/// <summary>
		/// Gets images for particles.
		/// This property must be set before particle injection.
		/// To prevent interference between textures in atlas all images must be padded with 16 pixels.
		/// </summary>
		public TextureAtlas Images { 
			get {
				return ps.Images;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		internal ParticleStream ( RenderSystem rs, RenderWorld renderWorld, ParticleSystem ps, bool sort, bool lightmap ) : base(rs)
		{
			this.renderWorld	=	renderWorld;
			this.ps				=	ps;
			particleCount		=	MAX_PARTICLES;
			sortParticles		=	sort;
			useLightmap			=	lightmap;

			paramsCB			=	new ConstantBuffer( Game.GraphicsDevice, typeof(PARAMS) );
			imagesCB			=	new ConstantBuffer( Game.GraphicsDevice, typeof(Vector4), MAX_IMAGES );

			injectionBuffer			=	new StructuredBuffer( Game.GraphicsDevice, typeof(Particle),		MAX_INJECTED, StructuredBufferFlags.None );
			simulationBuffer		=	new StructuredBuffer( Game.GraphicsDevice, typeof(Particle),		MAX_PARTICLES, StructuredBufferFlags.None );
			sortParticlesBuffer		=	new StructuredBuffer( Game.GraphicsDevice, typeof(Vector2),			MAX_PARTICLES, StructuredBufferFlags.None );
			deadParticlesIndices	=	new StructuredBuffer( Game.GraphicsDevice, typeof(uint),			MAX_PARTICLES, StructuredBufferFlags.Append );
			lightMapRegions			=	new StructuredBuffer( Game.GraphicsDevice, typeof(Vector4),			MAX_PARTICLES, StructuredBufferFlags.None );

			if (useLightmap) {
				this.lightmap	=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.Rgba16F,	(int)LightmapWidth, (int)LightmapHeight, false );
			}

			rs.Game.Reloading += LoadContent;
			LoadContent(this, EventArgs.Empty);

			//	initialize dead list :
			var device = Game.GraphicsDevice;

			device.SetComputeUnorderedAccess( 1, deadParticlesIndices.UnorderedAccess, 0 );
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

				SafeDispose( ref lightMapRegions );
				SafeDispose( ref injectionBuffer );
				SafeDispose( ref simulationBuffer );
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
			if (flag.HasFlag(Flags.DRAW))
			{
				if (flag.HasFlag(Flags.SOFT)) {
					ps.BlendState			=	BlendState.AlphaBlendOffScreen;
					ps.DepthStencilState	=	DepthStencilState.None;
					ps.Primitive			=	Primitive.PointList;
					ps.RasterizerState		=	RasterizerState.CullNone;
				}

				if (flag.HasFlag(Flags.HARD)) {
					ps.BlendState			=	BlendState.Opaque;
					ps.DepthStencilState	=	DepthStencilState.Default;
					ps.Primitive			=	Primitive.PointList;
					ps.RasterizerState		=	RasterizerState.CullNone;
				}

				if (flag.HasFlag(Flags.DUDV)) {
					ps.BlendState			=	BlendState.Additive;
					ps.DepthStencilState	=	DepthStencilState.None;
					ps.Primitive			=	Primitive.PointList;
					ps.RasterizerState		=	RasterizerState.CullNone;
				}


				if (flag.HasFlag(Flags.VELOCITY)) {
					ps.BlendState			=	BlendState.AlphaBlend;
					ps.DepthStencilState	=	DepthStencilState.None;
					ps.Primitive			=	Primitive.PointList;
					ps.RasterizerState		=	RasterizerState.CullNone;
				}

				if (flag.HasFlag(Flags.SOFT_SHADOW) || flag.HasFlag(Flags.HARD_SHADOW)) {

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

				if (flag.HasFlag(Flags.LIGHTMAP)) {
					ps.BlendState			=	BlendState.Opaque;
					ps.DepthStencilState	=	DepthStencilState.None;
					ps.Primitive			=	Primitive.PointList;
					ps.RasterizerState		=	RasterizerState.CullNone;
				}
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
		public void InjectParticle ( ref Particle particle )
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
		void SetupGPUParameters ( float stepTime, uint stepCount, RenderWorld renderWorld, Camera camera, Flags flags )
		{
			var deltaTime		=	stepTime;

			//	kill particles by applying very large delta:
			if (flags.HasFlag(Flags.SIMULATION) && requestKill) 
			{
				deltaTime	=	9999;
				stepCount	=	1;
				requestKill	=	false;
			}
			
			//	freeze particles :
			if (rs.FreezeParticles) 
			{
				deltaTime = 0;
			}

			//	fill constant data :
			PARAMS param		=	new PARAMS();

			var occlusionMatrix = Matrix.Identity;

			if (rs.RenderWorld.IrradianceVolume!=null)
			{
				occlusionMatrix = rs.RenderWorld.IrradianceVolume.WorldPosToTexCoord;
			}

			param.WorldToVoxelOffset	=	rs.Radiosity.GetWorldToVoxelOffset();
			param.WorldToVoxelScale		=	rs.Radiosity.GetWorldToVoxelScale();
			param.SkyAmbientLevel		=	new Color4(8,0,4,1);
			param.MaxParticles			=	0;
			param.DeltaTime				=	deltaTime;
			param.Gravity				=	new Vector4( this.Gravity, 0 );

			if (useLightmap) {
				param.LightMapSize	=	new Vector4( Lightmap.Width, Lightmap.Height, 1.0f/Lightmap.Width, 1.0f/Lightmap.Height );
			}

			param.MaxParticles		=	MAX_PARTICLES;
			param.CocBias			=	0;
			param.CocScale			=	0;
			param.IntegrationSteps	=	stepCount;

			param.IndirectLightFactor	=	rs.Radiosity.MasterIntensity;
			param.DirectLightFactor		=	rs.SkipDirectLighting ? 0 : 1;

			if (flags==Flags.INJECTION) 
			{
				param.MaxParticles	=	injectionCount;
			}

			//	copy to gpu :
			paramsCB.SetData( ref param );

			//	set DeadListSize to prevent underflow:
			if (flags==Flags.INJECTION) 
			{
				deadParticlesIndices.CopyStructureCount( paramsCB, Marshal.OffsetOf( typeof(PARAMS), "DeadListSize").ToInt32() );
			}

			//	assign constant buffers to pipeline :
			device.ComputeConstants	[ regParams			]	=	paramsCB;
			device.GfxConstants		[ regParams			]	=	paramsCB;

			device.ComputeConstants	[ regCamera			]	=	camera.CameraData;
			device.GfxConstants		[ regCamera			]	=	camera.CameraData;

			device.ComputeConstants	[ regCameraWeapon	]	=	renderWorld.WeaponCamera.CameraData;
			device.GfxConstants		[ regCameraWeapon	]	=	renderWorld.WeaponCamera.CameraData;

			device.ComputeConstants	[ regImages			]	=	imagesCB;
			device.GfxConstants		[ regImages			]	=	imagesCB;

			device.ComputeConstants	[ regDirectLight	]	=	rs.LightManager.DirectLightData;
			device.GfxConstants		[ regDirectLight	]	=	rs.LightManager.DirectLightData;

			device.ComputeConstants	[ regCascadeShadow	]	=	rs.LightManager.ShadowMap.GetCascadeShadowConstantBuffer();
			device.GfxConstants		[ regCascadeShadow	]	=	rs.LightManager.ShadowMap.GetCascadeShadowConstantBuffer();

			device.ComputeConstants	[ regFog			]	=	rs.Fog.FogData;
			device.GfxConstants		[ regFog			]	=	rs.Fog.FogData;
		}



		float timeAccumulator = 0;



		/// <summary>
		/// Updates particle properties.
		/// </summary>
		/// <param name="gameTime"></param>
		internal void Simulate ( GameTime gameTime, Camera camera )
		{
			var device	=	Game.GraphicsDevice;

			var view		=	camera.ViewMatrix;
			var projection	=	camera.ProjectionMatrix;


			timeAccumulator	+=	gameTime.ElapsedSec;

			//	to avoid time accumulator be to much :
			timeAccumulator	=	MathUtil.Clamp( timeAccumulator, -1, 1 );


			using ( new PixEvent("Particle Simulation") ) 
			{
				device.ResetStates();

				//
				//	Inject :
				//
				using (new PixEvent("Injection")) 
				{
					//injectionBuffer.SetData( injectionBufferCPU );
					injectionBuffer.UpdateData( injectionBufferCPU );

					device.ComputeResources[ reginjectionBuffer ] = injectionBuffer;
					device.SetComputeUnorderedAccess( regparticleBuffer,			simulationBuffer.UnorderedAccess,		0 );
					device.SetComputeUnorderedAccess( regdeadParticleIndicesPush,	deadParticlesIndices.UnorderedAccess, -1 );

					SetupGPUParameters( 0, 0, renderWorld, camera, Flags.INJECTION );

					device.PipelineState	=	factory[ (int)Flags.INJECTION ];
			
					//	GPU time ???? -> 0.0046
					device.Dispatch( MathUtil.IntDivUp( MAX_INJECTED, BLOCK_SIZE ) );

					ClearParticleBuffer();
				}


				//
				//	Simulate :
				//
				using (new PixEvent("Simulation")) 
				{
					if (!renderWorld.IsPaused && !rs.SkipParticlesSimulation) 
					{
						device.SetComputeUnorderedAccess( 0, simulationBuffer.UnorderedAccess,		0 );
						device.SetComputeUnorderedAccess( 1, deadParticlesIndices.UnorderedAccess, -1 );
						device.SetComputeUnorderedAccess( 2, sortParticlesBuffer.UnorderedAccess, 0 );
						
						float stepTime	= SimulationStepTime;
						uint  stepCount = 0;

						while ( timeAccumulator > stepTime ) 
						{
							stepCount ++;
							timeAccumulator -= stepTime;
						}

						SetupGPUParameters( stepTime, stepCount, renderWorld, camera, Flags.SIMULATION);

						device.PipelineState	=	factory[ (int)Flags.SIMULATION ];
	
						/// GPU time : 1.665 ms	 --> 0.38 ms
						device.Dispatch( MathUtil.IntDivUp( MAX_PARTICLES, BLOCK_SIZE ) );
					}
				}


				//
				//	Alloc Lightmap :
				//
				using (new PixEvent("Alloc LightMap")) 
				{
					if (useLightmap && !renderWorld.IsPaused && !rs.SkipParticlesSimulation) 
					{
						device.SetComputeUnorderedAccess( 0, simulationBuffer.UnorderedAccess,		0 );
						device.SetComputeUnorderedAccess( 1, deadParticlesIndices.UnorderedAccess, -1 );
						device.SetComputeUnorderedAccess( 2, sortParticlesBuffer.UnorderedAccess, 0 );
						device.SetComputeUnorderedAccess( 3, lightMapRegions.UnorderedAccess, 0 );

						SetupGPUParameters( 0, 0, renderWorld, camera, Flags.ALLOC_LIGHTMAP );

						device.PipelineState	=	factory[ (int)Flags.ALLOC_LIGHTMAP ];
	
						device.Dispatch( 1, 1, 1 );//*/
					}
				}


				if (rs.ShowParticles) 
				{
					rs.Counters.DeadParticles	=	deadParticlesIndices.GetStructureCount();
				}

				device.ResetStates();

				//
				//	Sort :
				//
				if (sortParticles) 
				{
					using ( new PixEvent( "Sort" ) ) 
					{
						rs.BitonicSort.Sort( sortParticlesBuffer );
					}
				}

			}
		}



		/// <summary>
		/// 
		/// </summary>
		void RenderGeneric ( string passName, GameTime gameTime, Camera camera, Viewport viewport, RenderTargetSurface colorTarget, DepthStencilSurface depthTarget, ShaderResource depthValues, Flags flags )
		{
			var device	=	Game.GraphicsDevice;

			using ( new PixEvent(passName) ) {

				device.ResetStates();

				//
				//	Setup images :
				//
				if (Images!=null && !Images.IsDisposed) 
				{
					imagesCB.SetData( Images.GetNormalizedRectangles( MAX_IMAGES ) );
				}

				SetupGPUParameters( 0, 0, renderWorld, camera, flags );

				//
				//	Render
				//
				using (new PixEvent("Drawing")) 
				{
					//	target and viewport :
					device.SetTargets( depthTarget, colorTarget );
					device.SetScissorRect( viewport.Bounds );
					device.SetViewport( viewport );

					//	sampler & textures :
					device.GfxSamplers		[ regSampler ]	=	SamplerState.LinearClamp4Mips;
					device.ComputeSamplers	[ regSampler ]	=	SamplerState.LinearClamp4Mips;

					device.GfxResources[ regTexture 			]	=	Images==null? rs.WhiteTexture.Srv : Images.Texture.Srv;
					device.GfxResources[ reginjectionBuffer		]	=	simulationBuffer	;
					device.GfxResources[ regparticleBufferGS	]	=	simulationBuffer	;
					device.GfxResources[ regsortParticleBufferGS]	=	sortParticlesBuffer ;
					device.GfxResources[ regDepthValues			]	=	depthValues			;
					device.GfxResources[ regColorTemperature	]	=	ps.ColorTempMap.Srv ;
					device.GfxResources[ regFogVolume			]	=	rs.Fog.FogGrid;

					if (flags.HasFlag(Flags.LIGHTMAP) || flags.HasFlag(Flags.HARD)) 
					{
						device.GfxResources[ regClusterTable	]	=	rs.LightManager.LightGrid.GridTexture		;
						device.GfxResources[ regLightIndexTable	]	=	rs.LightManager.LightGrid.IndexDataGpu		;
						device.GfxResources[ regLightDataTable	]	=	rs.LightManager.LightGrid.LightDataGpu		;
						device.GfxResources[ regShadowMap		]	=	rs.LightManager.ShadowMap.ShadowTexture		;

						device.GfxResources[ regShadowMask		]	=	rs.LightManager.ShadowMap.ParticleShadowTexture	;

						device.GfxSamplers[ regSampler			]	=	SamplerState.LinearWrap						;
						device.GfxSamplers[ regShadowSampler	]	=	SamplerState.ShadowSampler					;
					}

					if (flags.HasFlag(Flags.SOFT)) 
					{
						device.GfxResources[ regLightMap ]	=	Lightmap;
					}

					if (flags.HasFlag(Flags.LIGHTMAP) || flags.HasFlag(Flags.HARD)) 
					{
						device.GfxResources[ regIrradianceVolumeL0 ]	= 	rs.Radiosity.LightVolumeL0	;
						device.GfxResources[ regIrradianceVolumeL1 ]	= 	rs.Radiosity.LightVolumeL1	;
						device.GfxResources[ regIrradianceVolumeL2 ]	= 	rs.Radiosity.LightVolumeL2	;
						device.GfxResources[ regIrradianceVolumeL3 ]	= 	rs.Radiosity.LightVolumeL3	;
					}

					device.GfxResources[ regLightMapRegionsGS ]	=	lightMapRegions;

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
		internal void RenderSoft ( GameTime gameTime, Camera camera, StereoEye stereoEye, HdrFrame viewFrame )
		{
			if (rs.SkipParticles) {
				return;
			}

			var view		=	camera.ViewMatrix;
			var projection	=	camera.ProjectionMatrix;

			var colorTarget	=	viewFrame.SoftParticlesFront.Surface;
			var depthSource	=	viewFrame.DepthBuffer;

			var viewport	=	new Viewport( 0, 0, colorTarget.Width, colorTarget.Height );

			device.Clear( colorTarget, new Color4(0,0,0,1) );

			RenderGeneric( "Soft Particles", gameTime, camera, viewport, colorTarget, null, depthSource, Flags.DRAW|Flags.SOFT );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		internal void RenderDuDv ( GameTime gameTime, Camera camera, StereoEye stereoEye, HdrFrame viewFrame )
		{
			if (rs.SkipParticles) {
				return;
			}

			var view		=	camera.ViewMatrix;
			var projection	=	camera.ProjectionMatrix;

			var colorTarget	=	viewFrame.DistortionBuffer.Surface;
			var depthSource	=	viewFrame.DepthBuffer;

			var viewport	=	new Viewport( 0, 0, colorTarget.Width, colorTarget.Height );

			RenderGeneric( "DuDv Particles", gameTime, camera, viewport, colorTarget, null, depthSource, Flags.DRAW|Flags.DUDV );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		internal void RenderVelocity ( GameTime gameTime, Camera camera, StereoEye stereoEye, HdrFrame viewFrame )
		{
			if (rs.SkipParticles) {
				return;
			}

			var view		=	camera.ViewMatrix;
			var projection	=	camera.ProjectionMatrix;

			var colorTarget	=	viewFrame.ParticleVelocity.Surface;
			var depthSource	=	viewFrame.DepthBuffer;

			var viewport	=	new Viewport( 0, 0, colorTarget.Width, colorTarget.Height );

			RenderGeneric( "Velocity Particles", gameTime, camera, viewport, colorTarget, null, depthSource, Flags.DRAW|Flags.VELOCITY );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		internal void RenderHard ( GameTime gameTime, Camera camera, StereoEye stereoEye, HdrFrame viewFrame )
		{
			if (rs.SkipParticles) {
				return;
			}

			var view		=	camera.ViewMatrix;
			var projection	=	camera.ProjectionMatrix;

			var colorTarget	=	viewFrame.HdrTarget.Surface;
			var depthTarget	=	viewFrame.DepthBuffer.Surface;

			var viewport	=	new Viewport( 0, 0, colorTarget.Width, colorTarget.Height );

			RenderGeneric( "Hard Particles", gameTime, camera, viewport, colorTarget, depthTarget, null, Flags.DRAW|Flags.HARD );
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		internal void RenderLightMap ( GameTime gameTime, Camera camera )
		{
			if (rs.SkipParticles) {
				return;
			}

			rs.Device.Clear( lightmap.Surface, Color4.Black );

			var view		=	camera.ViewMatrix;
			var projection	=	camera.ProjectionMatrix;

			var colorTarget	=	lightmap.Surface;

			var viewport	=	new Viewport( 0, 0, lightmap.Width, lightmap.Height );

			RenderGeneric( "Particles Light", gameTime, camera, viewport, colorTarget, null, null, Flags.DRAW|Flags.LIGHTMAP );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		internal void RenderBasisLight ( GameTime gameTime )
		{
			
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		internal void RenderShadow ( GameTime gameTime, Viewport viewport, Camera camera, RenderTargetSurface particleShadow, DepthStencilSurface depthBuffer, bool soft )
		{
			if (rs.SkipParticleShadows) {
				return;
			}

			var colorTarget	=	particleShadow;
			var depthTarget	=	depthBuffer;

			var flags		=	Flags.DRAW | (soft ? Flags.SOFT_SHADOW : Flags.HARD_SHADOW);

			RenderGeneric( "Particles Shadow", gameTime, camera, viewport, colorTarget, depthTarget, null, flags );
		}
	}
}
