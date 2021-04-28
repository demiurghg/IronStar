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

		[AECategory("General")]  [Config]	public bool UsePointShadowSampling { get; set; } = false;

		[AECategory("Cascade Shadows")]  [Config]	public bool SnapShadowmapCascades { get; set; } = true;
		[AECategory("Cascade Shadows")]  [Config]	public bool LockLessDetailedSplit { get; set; } = false;

		[AECategory("Cascade Shadows")]  [Config]	public float ShadowGradientBiasX { get; set; } = 1;
		[AECategory("Cascade Shadows")]  [Config]	public float ShadowGradientBiasY { get; set; } = 1;

		[AECategory("Cascade Shadows")]  [Config]	public float ShadowCascadeDepth { get; set; } = 1024;
		[AECategory("Cascade Shadows")]  [Config]	public float ShadowCascadeFactor { get; set; } = 3;
		[AECategory("Cascade Shadows")]  [Config]	public float ShadowCascadeSize { get; set; } = 4;

		bool biasDirty = true;

		float	spotSlopeBias = 2;
		int		spotDepthBias = 10;
		float	cascadeSlopeBias = 1;
		int		cascadeDepthBias = 1;
		[AECategory("Spot Shadows")]	[Config]	public float SpotSlopeBias { get { return spotSlopeBias; } set { if ( spotSlopeBias != value ) { biasDirty = true; spotSlopeBias = value; } } }
		[AECategory("Spot Shadows")]	[Config]	public int	 SpotDepthBias { get { return spotDepthBias; } set { if ( spotDepthBias != value ) { biasDirty = true; spotDepthBias = value; } } }
		[AECategory("Cascade Shadows")]	[Config]	public float CascadeSlopeBias { get { return cascadeSlopeBias; } set { if ( cascadeSlopeBias != value ) { biasDirty = true; cascadeSlopeBias = value; } } }
		[AECategory("Cascade Shadows")]	[Config]	public int	 CascadeDepthBias { get { return cascadeDepthBias; } set { if ( cascadeDepthBias != value ) { biasDirty = true; cascadeDepthBias = value; } } }
		/*float csmSlopeBias0 = 1;
		float csmSlopeBias1 = 1;
		float csmSlopeBias2 = 1;
		float csmSlopeBias3 = 1;
		int csmDepthBias0 = 1;
		int csmDepthBias1 = 1;
		int csmDepthBias2 = 1;
		int csmDepthBias3 = 1;
		[AECategory("Shadows")]  [Config]	public float CSMSlopeBias0 { get { return csmSlopeBias0; } set { if ( csmSlopeBias0 != value ) { biasDirty = true; csmSlopeBias0 = value; } } }
		[AECategory("Shadows")]  [Config]	public float CSMSlopeBias1 { get { return csmSlopeBias1; } set { if ( csmSlopeBias1 != value ) { biasDirty = true; csmSlopeBias1 = value; } } }
		[AECategory("Shadows")]  [Config]	public float CSMSlopeBias2 { get { return csmSlopeBias2; } set { if ( csmSlopeBias2 != value ) { biasDirty = true; csmSlopeBias2 = value; } } }
		[AECategory("Shadows")]  [Config]	public float CSMSlopeBias3 { get { return csmSlopeBias3; } set { if ( csmSlopeBias3 != value ) { biasDirty = true; csmSlopeBias3 = value; } } }
		[AECategory("Shadows")]  [Config]	public int CSMDepthBias0 { get { return csmDepthBias0; } set { if ( csmDepthBias0 != value ) { biasDirty = true; csmDepthBias0 = value; } } }
		[AECategory("Shadows")]  [Config]	public int CSMDepthBias1 { get { return csmDepthBias1; } set { if ( csmDepthBias1 != value ) { biasDirty = true; csmDepthBias1 = value; } } }
		[AECategory("Shadows")]  [Config]	public int CSMDepthBias2 { get { return csmDepthBias2; } set { if ( csmDepthBias2 != value ) { biasDirty = true; csmDepthBias2 = value; } } }
		[AECategory("Shadows")]  [Config]	public int CSMDepthBias3 { get { return csmDepthBias3; } set { if ( csmDepthBias3 != value ) { biasDirty = true; csmDepthBias3 = value; } } }*/


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


		public void Update ( GameTime gameTime, LightSet lightSet, IEnumerable<RenderInstance> instances )
		{
			CreateResourcesIfNecessary();
		}
	}
}
