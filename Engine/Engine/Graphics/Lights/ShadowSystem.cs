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



		public ShadowSystem( RenderSystem rs ) : base( rs )
		{
		}


		public override void Initialize()
		{
			CreateResourcesIfNecessary();
		}

		
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref shadowMap );
			}

			base.Dispose( disposing );
		}

		
		/*-----------------------------------------------------------------------------------------------
		 *	Private stuff :
		-----------------------------------------------------------------------------------------------*/

		public void RenderShadows ( GameTime gameTime, Camera camera, RenderWorld rw )
		{
			CreateResourcesIfNecessary();

			RemoveSpotLights( rw.LightSet.SpotLights );

			AllocateShadowRegions( rw.LightSet.SpotLights );

			UpdateVisibility( rw, rw.LightSet.SpotLights );

			var lightList = rw.LightSet.SpotLights
							.Where( s => s.IsContentDirty )
							.ToList();

			RenderShadowsInternal( rw, lightList, InstanceGroup.NotWeapon );

			//shadowMap.RenderShadowMaps( gameTime, camera, rs, rw, rw.LightSet );
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


		void RemoveSpotLights( IEnumerable<SpotLight> spotLights )
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
		void AllocateShadowRegions( IEnumerable<SpotLight> spotLights )
		{
			try 
			{
				foreach ( var spotLight in spotLights )
				{
					if (spotLight.IsRegionDirty)
					{
						shadowMap.Allocator.Free( spotLight.ShadowRegion );
					}
				}

				bool refresh = shadowMap.Allocator.IsEmpty;

				foreach ( var spotLight in spotLights )
				{
					if (spotLight.IsRegionDirty || refresh)
					{
						var shadowRegionSize		=	shadowMap.GetShadowRegionSize( spotLight.DetailLevel );
						spotLight.ShadowRegion		=	shadowMap.Allocator.Alloc( shadowRegionSize, spotLight );
						spotLight.ShadowScaleOffset	=	shadowMap.GetScaleOffset( spotLight.ShadowRegion );
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
		void UpdateVisibility( RenderWorld rw, IEnumerable<SpotLight> lights )
		{
			if (rw.SceneBvhTree==null) return;

			foreach ( var light in lights )
			{
				var frustum	=	new BoundingFrustum( light.SpotView * light.Projection );
				var newList	=	rw.SceneBvhTree.Traverse( bbox => frustum.Contains( bbox ) );
				var added	=	newList.Except( light.ShadowCasters );
				var removed	=	light.ShadowCasters.Except( newList );

				light.ShadowCasters	=	new RenderList(newList);

				if (added.Any() || removed.Any())
				{
					light.IsContentDirty = true;
				}

				light.IsContentDirty = true;
			}
		}


		/// <summary>
		/// Renders shadows
		/// </summary>
		void RenderShadowsInternal(	RenderWorld rw, IEnumerable<SpotLight> lights, InstanceGroup group )
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
				var context	=	new ShadowContext( rs, shadowCamera, light, depthBuffer.Surface, shadowTexture.Surface );

				rs.SceneRenderer.RenderShadowMap( context, light.ShadowCasters, group, false );
			}

			//	render shadow mask :
			foreach ( var light in lights )
			{
				var name	=	light.SpotMaskName;
				var clip	=	lightSet.SpotAtlas.GetClipByName( name );

				if (clip!=null) 
				{
					var dstRegion	=	light.ShadowRegion;
					var	srcRegion	=	lightSet.SpotAtlas.AbsoluteRectangles[ clip.FirstIndex ];
					rs.Filter2.CopyColor( maskTexture.Surface, lightSet.SpotAtlas.Texture.Srv, dstRegion, srcRegion, Color.White );
				} 
				else 
				{
					var dstRegion	=	light.ShadowRegion;
					rs.Filter2.RenderSpot( maskTexture.Surface, dstRegion, Color.White );
				}
			}

			//	render particle shadows :

			//	copy regions :
			shadowMap.CopyShadowRegionToLowRes( regions );
		}
	}
}
