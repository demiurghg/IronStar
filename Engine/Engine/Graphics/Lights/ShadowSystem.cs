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

namespace Fusion.Engine.Graphics 
{
	enum CascadeUpdateMode
	{
		None,
		Interleave1122,
		Interleave1244,
	}

	/// <summary>
	/// Shadow render context
	/// </summary>
	internal class ShadowSystem : RenderComponent
	{
		public const int MaxCascades		= 4;
		
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
		List<IShadowProvider> lightListToRender;


		public ShadowSystem( RenderSystem rs ) : base( rs )
		{
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

		public void RenderShadows ( GameTime gameTime, Camera camera, RenderWorld rw )
		{
			CreateResourcesIfNecessary();

			ComputeCascadeMatricies( cascades[0], camera, rw.LightSet );
			ComputeCascadeMatricies( cascades[1], camera, rw.LightSet );
			ComputeCascadeMatricies( cascades[2], camera, rw.LightSet );
			ComputeCascadeMatricies( cascades[3], camera, rw.LightSet );

			var lightList = new List<IShadowProvider>();
			lightList.AddRange( cascades );
			lightList.AddRange( rw.LightSet.SpotLights );

			RemoveSpotLights( lightList );

			AllocateShadowRegions( lightList );

			UpdateVisibility( rw, lightList );

			lightListToRender = lightList
							.Where( s => s.IsShadowDirty )
							.ToList();

			RenderShadowsInternal( gameTime, rw, lightListToRender, InstanceGroup.NotWeapon );
		}


		public void RenderParticleShadows(GameTime gameTime, Camera camera, RenderWorld rw)
		{
			if (lightListToRender!=null)
			{
				RenderParticleShadowsInternal( gameTime, rw, lightListToRender ); 
			}
		}


		public ShadowCascade GetCascade ( int index ) 
		{
			if (index<0 || index>=MaxCascades) 
			{
				throw new ArgumentOutOfRangeException("index", "index must be within range 0.." + (MaxCascades-1).ToString() );
			}
			
			return cascades[index];
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Private stuff :
		-----------------------------------------------------------------------------------------------*/

		/// <summary>
		/// Recreates shadows if necessary
		/// </summary>
		private void CreateResourcesIfNecessary()
		{
			if (shadowQualityDirty)
			{
				SafeDispose( ref shadowMap );
				shadowMap			=	new ShadowMap( rs, ShadowQuality );
				shadowQualityDirty	=	false;

				cascades[0]	=	new ShadowCascade(0, shadowMap.MaxRegionSize, 0, Color.White);
				cascades[1]	=	new ShadowCascade(1, shadowMap.MaxRegionSize, 0, new Color(255,0,0) );
				cascades[2]	=	new ShadowCascade(2, shadowMap.MaxRegionSize, 1, new Color(0,255,0) );
				cascades[3]	=	new ShadowCascade(3, shadowMap.MaxRegionSize, 1, new Color(0,0,255) );
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
			}
		}


		void RemoveSpotLights( IEnumerable<IShadowProvider> spotLights )
		{
			var blocks = shadowMap.Allocator.GetAllocatedBlockInfo();

			foreach ( var block in blocks )
			{
				if ( !spotLights.Contains( block.Tag ) )
				{
					shadowMap.Allocator.Free( block.Region );
				}
			}
		}


		/// <summary>
		/// Allocates and reallocates shadow regions when LOD is changed.
		/// </summary>
		void AllocateShadowRegions( IEnumerable<IShadowProvider> spotLights )
		{
			try 
			{
				foreach ( var spotLight in spotLights )
				{
					if (spotLight.IsRegionDirty)
					{
						shadowMap.Allocator.Free( spotLight );
					}
				}

				bool refresh = shadowMap.Allocator.IsEmpty;

				foreach ( var spotLight in spotLights )
				{
					if (spotLight.IsRegionDirty || refresh)
					{
						var shadowRegionSize		=	shadowMap.GetShadowRegionSize( spotLight.ShadowLod );
						var shadowRegion			=	shadowMap.Allocator.Alloc( shadowRegionSize, spotLight );

						spotLight.SetShadowRegion( shadowRegion, shadowMap.ShadowMapSize );
					}
				}
			} 
			catch ( Exception e )
			{
				Log.Warning(e.Message);
			}
		}


		/// <summary>
		/// Updates shadow caster visibility for each light
		/// </summary>
		/// <param name="lights"></param>
		void UpdateVisibility( RenderWorld rw, IEnumerable<IShadowProvider> lights )
		{
			if (rw.SceneBvhTree==null) return;

			foreach ( var light in lights )
			{
				var frustum	=	new BoundingFrustum( light.ViewMatrix * light.ProjectionMatrix );
				var newList	=	rw.SceneBvhTree.Traverse( bbox => frustum.Contains( bbox ) );

				var added	=	newList.Except( light.ShadowCasters );
				var removed	=	light.ShadowCasters.Except( newList );

				light.ShadowCasters.Clear();
				light.ShadowCasters.AddRange( newList );

				if (added.Any() || removed.Any() || newList.Any( ri => ri.IsShadowDirty ) )
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


		/// <summary>
		/// Renders shadows
		/// </summary>
		void RenderShadowsInternal(	GameTime gameTime, RenderWorld rw, IEnumerable<IShadowProvider> lights, InstanceGroup group )
		{
			var shadowCamera	=	rw.ShadowCamera;
			var depthBuffer		=	shadowMap.DepthBuffer;
			var shadowTexture	=	shadowMap.ShadowTexture;
			var maskTexture		=	shadowMap.ParticleShadowTexture;
			var lightSet		=	rw.LightSet;

			var regions		=	lights
							.Select( lt => lt.ShadowRegion )
							.ToArray();

			//	clear shadow maps :
			shadowMap.ClearShadowRegions( regions );

			//	render shadow map :
			foreach ( var light in lights )
			{
				shadowCamera.ViewMatrix			=	light.ViewMatrix;
				shadowCamera.ProjectionMatrix	=	light.ProjectionMatrix;

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
					rs.Filter2.CopyColor( maskTexture.Surface, lightSet.SpotAtlas.Texture.Srv, dstRegion, srcRegion, Color.White );
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

			data.CascadeViewProjection0		=	this.GetCascade( 0 ).ViewProjectionMatrix;
			data.CascadeViewProjection1		=	this.GetCascade( 1 ).ViewProjectionMatrix;
			data.CascadeViewProjection2		=	this.GetCascade( 2 ).ViewProjectionMatrix;
			data.CascadeViewProjection3		=	this.GetCascade( 3 ).ViewProjectionMatrix;

			data.CascadeGradientMatrix0		=	this.GetCascade( 0 ).ComputeGradientMatrix();
			data.CascadeGradientMatrix1		=	this.GetCascade( 1 ).ComputeGradientMatrix();
			data.CascadeGradientMatrix2		=	this.GetCascade( 2 ).ComputeGradientMatrix();
			data.CascadeGradientMatrix3		=	this.GetCascade( 3 ).ComputeGradientMatrix();

			data.CascadeScaleOffset0		=	this.GetCascade( 0 ).RegionScaleOffset;
			data.CascadeScaleOffset1		=	this.GetCascade( 1 ).RegionScaleOffset;
			data.CascadeScaleOffset2		=	this.GetCascade( 2 ).RegionScaleOffset;
			data.CascadeScaleOffset3		=	this.GetCascade( 3 ).RegionScaleOffset;

			if (float.IsNaN(data.CascadeViewProjection0.M11))
			{
				//	bad data, reset 
				data = new CASCADE_SHADOW();
			}

			constCascadeShadow.SetData(ref data);

			return constCascadeShadow;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public ConstantBuffer GetCascadeShadowConstantBuffer ()
		{
			return constCascadeShadow;
		}


	}
}
