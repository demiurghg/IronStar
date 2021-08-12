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
using Fusion.Widgets.Advanced;
using System.Runtime.CompilerServices;
using Fusion.Engine.Graphics.Lights;
using Fusion.Engine.Graphics.Ubershaders;
using Wintellect.PowerCollections;
using Fusion.Core.Collection;

namespace Fusion.Engine.Graphics 
{
	enum CascadeUpdateMode
	{
		None,
		Interleave1122,
		Interleave1244,
	}


	enum ShadowPriority
	{
		Urgent,
		High,
		Medium,
		Low,
	}
	
	
	internal class ShadowSystem : RenderComponent
	{
		public const int MaxCascades		=	4;
		public const int MaxSPF				=	32;

		int frameCounter = 0;
		
		[AECategory("General")]  [Config]	public QualityLevel ShadowQuality 
		{ 
			get { return shadowQualityLevel; }
			set 
			{ 
				if (shadowQualityLevel!=value)
				{
					shadowQualityLevel = value;
					shadowQualityDirty = true;
				}
			}
		}

		bool shadowQualityDirty = true;
		QualityLevel shadowQualityLevel	= QualityLevel.Medium;

		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential, Pack=4)]
		public struct CASCADE_SHADOW 
		{
			public Matrix	CascadeViewProjection0	;
			public Matrix	CascadeViewProjection1	;
			public Matrix	CascadeViewProjection2	;
			public Matrix	CascadeViewProjection3	;
			public Matrix	CascadeGradientMatrix0	;
			public Matrix	CascadeGradientMatrix1	;
			public Matrix	CascadeGradientMatrix2	;
			public Matrix	CascadeGradientMatrix3	;
			public Vector4	CascadeScaleOffset0		;
			public Vector4	CascadeScaleOffset1		;
			public Vector4	CascadeScaleOffset2		;
			public Vector4	CascadeScaleOffset3		;
		}

		[AECategory("Performance")]		[Config]	public bool UsePointShadowSampling { get; set; } = false;
		[AECategory("Performance")]		[Config]	public bool SkipShadowMasks { get; set; } = false;
		[AECategory("Performance")]		[Config]	public bool SkipParticleShadows { get; set; } = false;
		
		[AESlider(0,MaxSPF,1,1)]
		[AECategory("Performance")]		[Config]	public int  ShadowsPerFrame { get; set; } = 8;
		
		[AESlider(0,8,1,1)]
		[AECategory("Performance")]		[Config]	public int  MaxParticleShadowsLod { get; set; } = 2;
		[AECategory("Performance")]		[Config]	public CascadeUpdateMode CascadeUpdateMode { get; set; } = CascadeUpdateMode.Interleave1122;

		[AECategory("Debug")]			[Config]	public bool ShowSplits { get; set; } = false;
		[AECategory("Debug")]			[Config]	public bool UseHighResFogShadows { get; set; } = false;
		[AECategory("Debug")]			[Config]	public bool ClearEntireShadow { get; set; } = false;
		[AECategory("Debug")]			[Config]	public bool SkipBorders { get; set; } = false;
		[AECategory("Debug")]			[Config]	public bool SkipShadowCasterTracking { get; set; } = false;

		[AECategory("Cascade Shadows")] [Config]	public bool SnapShadowmapCascades { get; set; } = true;
		[AECategory("Cascade Shadows")] [Config]	public float ShadowGradientBiasX { get; set; } = 1;
		[AECategory("Cascade Shadows")] [Config]	public float ShadowGradientBiasY { get; set; } = 1;
		[AECategory("Cascade Shadows")] [Config]	public float ShadowCascadeDepth { get; set; } = 1024;
		[AECategory("Cascade Shadows")] [Config]	public float ShadowCascadeFactor { get; set; } = 3;
		[AECategory("Cascade Shadows")] [Config]	public float ShadowCascadeSize { get; set; } = 4;

		bool biasDirty = true;

		float	spotSlopeBias = 2;
		int		spotDepthBias = 10;
		float	cascadeSlopeBias = 1;
		int		cascadeDepthBias = 1;
		[AECategory("Spot Shadows")]	[Config]	public float SpotSlopeBias { get { return spotSlopeBias; } set { if ( spotSlopeBias != value ) { biasDirty = true; spotSlopeBias = value; } } }
		[AECategory("Spot Shadows")]	[Config]	public int	 SpotDepthBias { get { return spotDepthBias; } set { if ( spotDepthBias != value ) { biasDirty = true; spotDepthBias = value; } } }
		[AECategory("Cascade Shadows")]	[Config]	public float CascadeSlopeBias { get { return cascadeSlopeBias; } set { if ( cascadeSlopeBias != value ) { biasDirty = true; cascadeSlopeBias = value; } } }
		[AECategory("Cascade Shadows")]	[Config]	public int	 CascadeDepthBias { get { return cascadeDepthBias; } set { if ( cascadeDepthBias != value ) { biasDirty = true; cascadeDepthBias = value; } } }


		public ShadowMap ShadowMap { get { return shadowMap; } }
		ShadowMap shadowMap;

		public RasterizerState CascadeShadowRasterizerState { get { return cascadeShadowRasterizerState; } }
		public RasterizerState SpotShadowRasterizerState { get { return spotShadowRasterizerState; } }
		RasterizerState cascadeShadowRasterizerState;
		RasterizerState spotShadowRasterizerState;
		ConstantBuffer	constCascadeShadow;

		readonly ShadowCascade[] cascades = new ShadowCascade[MaxCascades];
		readonly List<IShadowProvider> renderList;
		readonly ConcurrentPriorityQueue<int,IShadowProvider> renderQueue;



		public ShadowSystem( RenderSystem rs ) : base( rs )
		{
			renderQueue	=	new ConcurrentPriorityQueue<int,IShadowProvider>();
			renderList	=	new List<IShadowProvider>(MaxSPF);
		}



		public override void Initialize()
		{
			constCascadeShadow	=	new ConstantBuffer( device, typeof(CASCADE_SHADOW) );

			CreateResourcesIfNecessary();
		}



		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref shadowMap );
				SafeDispose( ref constCascadeShadow );
			}

			base.Dispose( disposing );
		}

		
		/*-----------------------------------------------------------------------------------------------
		 *	Private stuff :
		-----------------------------------------------------------------------------------------------*/

		[MethodImpl(MethodImplOptions.NoOptimization|MethodImplOptions.NoInlining)]
		public void RenderShadows ( GameTime gameTime, Camera camera, RenderWorld rw )
		{
			frameCounter++;

			var settingsChanged		=	CreateResourcesIfNecessary();

			rs.Stats.CascadeCount	=	cascades.Length;

			//	get list of visible lights :
			var lightList = new List<IShadowProvider>();
			lightList.AddRange( cascades );
			lightList.AddRange( rw.LightSet.SpotLights.Where( spot => spot.IsVisible ) );

			ComputeCascadeMatricies( cascades[0], camera, rw.LightSet );
			ComputeCascadeMatricies( cascades[1], camera, rw.LightSet );
			ComputeCascadeMatricies( cascades[2], camera, rw.LightSet );
			ComputeCascadeMatricies( cascades[3], camera, rw.LightSet );

			//	update visibility and track shadow caster changes :
			TrackShadowCastersVisibility( rw, lightList );

			//	enqueue light with priorities :
			foreach ( var light in lightList )
			{
				bool isLodChanged;

				if (shadowMap.IsShadowAllocated(light, out isLodChanged))
				{
					if (light.IsShadowDirty)
					{
						EnqueueShadow( light, ShadowPriority.High );
					}
					else if (isLodChanged)
					{
						EnqueueShadow( light, ShadowPriority.Medium );
					}
					else
					{
						EnqueueShadow( light, ShadowPriority.Low );
					}
				}
				else
				{
					EnqueueShadow( light, ShadowPriority.Urgent );
				}
			}

			//	create render list for first shadows :
			renderList.Clear();

			for (int i=0; i<ShadowsPerFrame; i++)
			{
				IShadowProvider light;
				if (renderQueue.TryDequeue(out light))
				{
					renderList.Add( light );
				}
			}

			AllocateShadows( renderList );

			RenderShadowsInternal( gameTime, rw, renderList, InstanceGroup.NotWeapon );
		}



		public void RenderParticleShadows(GameTime gameTime, Camera camera, RenderWorld rw)
		{
			if (renderList!=null)
			{
				//RenderParticleShadowsInternal( gameTime, rw, renderList ); 
			}
		}



		void EnqueueShadow( IShadowProvider shadow, ShadowPriority priority )
		{
			if (!renderQueue.Any( kv => kv.Value==shadow ))
			{
				int addition = 0;

				switch (priority)
				{
					case ShadowPriority.Urgent:	addition = frameCounter	- MaxSPF * 4;	break;
					case ShadowPriority.High:  	addition = frameCounter - MaxSPF * 2;	break;
					case ShadowPriority.Medium:	addition = frameCounter				;	break;
					case ShadowPriority.Low:   	addition = frameCounter + MaxSPF * 2;	break;
				}

				renderQueue.Enqueue( shadow.ShadowLod + addition, shadow );
			}
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Private stuff :
		-----------------------------------------------------------------------------------------------*/

		private bool CreateResourcesIfNecessary()
		{
			bool result = false;

			if (shadowQualityDirty)
			{
				SafeDispose( ref shadowMap );
				shadowMap			=	new ShadowMap( rs, ShadowQuality );
				shadowQualityDirty	=	false;

				cascades[0]	=	new ShadowCascade(0, shadowMap.MaxRegionSize, 0, Color.White);
				cascades[1]	=	new ShadowCascade(1, shadowMap.MaxRegionSize, 0, new Color(255,0,0) );
				cascades[2]	=	new ShadowCascade(2, shadowMap.MaxRegionSize, 0, new Color(0,255,0) );
				cascades[3]	=	new ShadowCascade(3, shadowMap.MaxRegionSize, 0, new Color(0,0,255) );

				result = true;
			}

			if (biasDirty)
			{
				spotShadowRasterizerState		=	RasterizerState.Create( CullMode.CullCW, FillMode.Solid, 0,0 ); 
				spotShadowRasterizerState.ScissorEnabled	=	true;
				spotShadowRasterizerState.DepthBias			=	spotDepthBias;
				spotShadowRasterizerState.SlopeDepthBias	=	spotSlopeBias;

				cascadeShadowRasterizerState		=	RasterizerState.Create( CullMode.CullCW, FillMode.Solid, 0,0 ); 
				cascadeShadowRasterizerState.ScissorEnabled	=	true;
				cascadeShadowRasterizerState.DepthBias		=	cascadeDepthBias;
				cascadeShadowRasterizerState.SlopeDepthBias	=	cascadeSlopeBias;

				//	make shaders reloaded
				if (Game.IsInitialized)
				{
					Game.Reload();
				}
				biasDirty = false;
				result = true;
			}

			return result;
		}


		/// <summary>
		/// Updates shadow caster visibility for each light
		/// </summary>
		/// <param name="lights"></param>
		void TrackShadowCastersVisibility( RenderWorld rw, IEnumerable<IShadowProvider> lights )
		{
			if (rw.SceneBvhTree==null) return;

			foreach ( var light in lights )
			{
				var frustum	=	new BoundingFrustum( light.ViewMatrix * light.ProjectionMatrix );
				var newList	=	rw.SceneBvhTree.Traverse( bbox => frustum.Contains( bbox ) );

				var added	=	newList.Except( light.ShadowCasters ).Any();
				var removed	=	light.ShadowCasters.Except( newList ).Any();
				var moved	=	newList.Any( ri => ri.IsShadowDirty );

				light.ShadowCasters.Clear();
				light.ShadowCasters.AddRange( newList );

				if ( added || removed || moved )
				{
					light.IsShadowDirty = true;
				}

				if (SkipShadowCasterTracking)
				{
					light.IsShadowDirty = true;
				}
			}

			foreach ( var ri in rw.Instances )
			{
				ri.ClearShadowDirty();
			}
		}



		void AllocateShadows( IEnumerable<IShadowProvider> lights )
		{
			foreach ( var light in lights )
			{
				shadowMap.AllocShadow( light );
			}
		}


		void RenderShadowsInternal(	GameTime gameTime, RenderWorld rw, IEnumerable<IShadowProvider> lights, InstanceGroup group )
		{
			var shadowCamera	=	rw.ShadowCamera;
			var depthBuffer		=	shadowMap.DepthBuffer;
			var shadowTexture	=	shadowMap.ShadowTexture;
			var maskTexture		=	shadowMap.ParticleShadowTexture;
			var lightSet		=	rw.LightSet;

			var regions	=	lights
							.Select( lt => lt.ShadowRegion )
							.ToArray();

			//	clear shadow maps :
			shadowMap.ClearShadowRegions( regions );

			//	render shadow map :
			foreach ( var light in lights )
			{
				rs.Stats.ShadowMapCount++;

				shadowCamera.ViewMatrix			=	light.ViewMatrix;
				shadowCamera.ProjectionMatrix	=	light.ProjectionMatrix;

				light.ShadowViewProjection		=	light.ViewMatrix * light.ProjectionMatrix;

				var context	=	new ShadowContext( rs, shadowCamera, light, depthBuffer.Surface, shadowTexture.Surface );

				rs.SceneRenderer.RenderShadowMap( context, light.ShadowCasters, group, false );

				light.IsShadowDirty = false;
			}

			//	render shadow mask :
			foreach ( var light in lights )
			{
				var name	=	light.ShadowMaskName;
				var clip	=	lightSet.SpotAtlas?.GetClipByName( name );

				if (clip!=null) 
				{
					var dstRegion	=	light.ShadowRegion;
					var	srcRegion	=	lightSet.SpotAtlas.AbsoluteRectangles[ clip.FirstIndex ];
					rs.Filter2.CopyColor( maskTexture.Surface, lightSet.SpotAtlas?.Texture?.Srv, dstRegion, srcRegion, Color.White );
				} 
				else 
				{
					var dstRegion	=	light.ShadowRegion;
					rs.Filter2.RenderBorder( maskTexture.Surface, dstRegion, Color.White );
				}
			}

			//	copy regions :
			shadowMap.CopyShadowRegionToLowRes( regions );
		}



		void RenderParticleShadowsInternal(	GameTime gameTime, RenderWorld rw, IEnumerable<IShadowProvider> lights )
		{
			if (SkipParticleShadows)
			{
				return;
			}

			var shadowCamera	=	rw.ShadowCamera;
			var depthBuffer		=	shadowMap.DepthBuffer;
			var shadowTexture	=	shadowMap.ShadowTexture;
			var maskTexture		=	shadowMap.ParticleShadowTexture;
			var lightSet		=	rw.LightSet;

			//	render particle shadows :
			foreach ( var light in lights )
			{
				if (light.ShadowLod <= MaxParticleShadowsLod) 
				{
					var vp		= new Viewport( light.ShadowRegion );

					shadowCamera.ViewMatrix			=	light.ViewMatrix;
					shadowCamera.ProjectionMatrix	=	light.ProjectionMatrix;

					rs.RenderWorld.ParticleSystem.RenderShadow( gameTime, vp, shadowCamera, maskTexture.Surface, depthBuffer.Surface );
				}
			}
		}


		/*-----------------------------------------------------------------------------------------
		 *	Shadow Cascade stuff :
		-----------------------------------------------------------------------------------------*/

		void ComputeCascadeMatricies ( ShadowCascade cascade, Camera camera, LightSet lightSet )
		{
			var camMatrix		=	camera.CameraMatrix;
			var viewPos			=	camera.CameraPosition;
			var lightDir		=	lightSet.DirectLight.Direction;
			var viewMatrix		=	camera.ViewMatrix;
			var lessDetailed	=	cascades.Length-1;

			var splitSize		=	rs.ShadowSystem.ShadowCascadeSize;
			var splitFactor		=	rs.ShadowSystem.ShadowCascadeFactor;
			var projDepth		=	rs.ShadowSystem.ShadowCascadeDepth;
			var splitOffset		=	0;

			lightDir.Normalize();

			var	smSize			=	cascade.ShadowRegion.Width; //	width == height

			float	offset		=	splitOffset * (float)Math.Pow( splitFactor, cascade.Index );
			float	radius		=	splitSize   * (float)Math.Pow( splitFactor, cascade.Index );

			Vector3 viewDir		=	camMatrix.Forward.Normalized();
			Vector3	origin		=	viewPos + viewDir * offset;

			Matrix	lightRot	=	Matrix.LookAtRH( Vector3.Zero, Vector3.Zero + lightDir, Vector3.UnitY );
			Matrix	lightRotI	=	Matrix.Invert( lightRot );
			Vector3	lsOrigin	=	Vector3.TransformCoordinate( origin, lightRot );
			float	snapValue	=	4 * radius / smSize;

			if (SnapShadowmapCascades) 
			{
				lsOrigin.X		=	(float)Math.Round(lsOrigin.X / snapValue) * snapValue;
				lsOrigin.Y		=	(float)Math.Round(lsOrigin.Y / snapValue) * snapValue;
			}
			//lsOrigin.Z			=	(float)Math.Round(lsOrigin.Z / snapValue) * snapValue;
			origin				=	Vector3.TransformCoordinate( lsOrigin, lightRotI );//*/

			var view			=	Matrix.LookAtRH( origin, origin + lightDir, Vector3.UnitY );
			var projection		=	Matrix.OrthoRH( radius*2, radius*2, -projDepth/2, projDepth/2);

			cascade.ViewMatrix			=	view;
			cascade.ProjectionMatrix	=	projection;	  
		}


		
		public ConstantBuffer UpdateCascadeShadowConstantBuffer ()
		{
			var data = new CASCADE_SHADOW();

			data.CascadeViewProjection0		=	this.GetCascade( 0 ).ShadowViewProjection;
			data.CascadeViewProjection1		=	this.GetCascade( 1 ).ShadowViewProjection;
			data.CascadeViewProjection2		=	this.GetCascade( 2 ).ShadowViewProjection;
			data.CascadeViewProjection3		=	this.GetCascade( 3 ).ShadowViewProjection;

			data.CascadeGradientMatrix0		=	this.GetCascade( 0 ).ComputeGradientMatrix();
			data.CascadeGradientMatrix1		=	this.GetCascade( 1 ).ComputeGradientMatrix();
			data.CascadeGradientMatrix2		=	this.GetCascade( 2 ).ComputeGradientMatrix();
			data.CascadeGradientMatrix3		=	this.GetCascade( 3 ).ComputeGradientMatrix();

			data.CascadeScaleOffset0		=	this.GetCascade( 0 ).RegionScaleTranslate;
			data.CascadeScaleOffset1		=	this.GetCascade( 1 ).RegionScaleTranslate;
			data.CascadeScaleOffset2		=	this.GetCascade( 2 ).RegionScaleTranslate;
			data.CascadeScaleOffset3		=	this.GetCascade( 3 ).RegionScaleTranslate;

			if (float.IsNaN(data.CascadeViewProjection0.M11))
			{
				//	bad data, reset 
				data = new CASCADE_SHADOW();
			}

			constCascadeShadow.SetData(ref data);

			return constCascadeShadow;
		}


		public ShadowCascade GetCascade ( int index ) 
		{
			if (index<0 || index>=MaxCascades) 
			{
				throw new ArgumentOutOfRangeException("index", "index must be within range 0.." + (MaxCascades-1).ToString() );
			}
			
			return cascades[index];
		}


		public ConstantBuffer GetCascadeShadowConstantBuffer ()
		{
			return constCascadeShadow;
		}
	}
}
