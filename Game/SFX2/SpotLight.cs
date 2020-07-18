using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using Fusion.Engine.Graphics;
using RSSpotLight = Fusion.Engine.Graphics.SpotLight;
using Fusion.Core.Shell;

namespace IronStar.SFX2
{
	public class SpotLight : Component, ITransformable
	{
		[AECategory("Light Color")]
		[AEDisplayName("Light Color")]
		public Color LightColor { get; set; } = Color.White;

		[AECategory("Light Color")]
		[AEDisplayName("Intensity")]
		[AEValueRange(0, 5000, 10, 1)]
		public float LightIntensity { get; set; } = 8;


		[AECategory("Spot Shape")]
		[AEDisplayName("Outer Radius")]
		[AEValueRange(0, 100, 1, 0.125f)]
		public float OuterRadius { get; set; } = 5;
		
		[AECategory("Spot Shape")]
		[AEDisplayName("Tube Radius")]
		[AEValueRange(0, 50, 1, 0.125f)]
		public float TubeRadius { get; set; } = 0.125f;
		
		[AECategory("Spot Shape")]
		[AEDisplayName("Tube Length")]
		[AEValueRange(0, 50, 1, 0.125f)]
		public float TubeLength { get; set; } = 0.0f;
		
		[AECategory("Spot Shape")]
		[AEValueRange(0, 4, 1/4f, 1/64f)]
		public float NearPlane { get; set; } = 0.125f;
		
		[AECategory("Spot Shape")]
		[AEValueRange(0, 100, 1, 1/8f)]
		public float FarPlane { get; set; } = 5;
		
		[AECategory("Spot Shape")]
		[AEValueRange(0, 150, 15, 1)]
		public float FovVertical { get; set; } = 60;
		
		[AECategory("Spot Shape")]
		[AEValueRange(0, 150, 15, 1)]
		public float FovHorizontal { get; set; } = 60;


		[AECategory("Global Illumination")]
		[AEDisplayName("Enable GI")]
		public bool EnableGI { get; set; } = false;


		[AECategory("Spot Shadow")]
		[AEDisplayName("Spot Mask")]
		[AEAtlasImage("spots/spots")]
		public string SpotMaskName { get; set; } = "";
		
		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow LOD Bias")]
		[AEValueRange(0, 8, 1, 1)]
		public int LodBias { get; set; } = 0;
		
		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow Depth Bias")]
		[AEValueRange(0, 1/512f, 1/8192f, 1/16384f)]
		public float DepthBias { get; set; } = 1f / 1024f;

		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow Slope Bias")]
		[AEValueRange(0, 8, 1, 0.125f/4.0f)]
		public float SlopeBias { get; set; } = 2;


		LightSet lightSet;
		RSSpotLight light;
		public RSSpotLight Light { get { return light; } }


		public SpotLight ()
		{
		}										


		public override void Added( GameState gs, Entity entity )
		{
			base.Added( gs, entity );

			lightSet	=	gs.GetService<RenderSystem>().RenderWorld.LightSet;
			light		=	new RSSpotLight();
			lightSet.SpotLights.Add ( light );
		}


		public override void Removed( GameState gs, Entity entity )
		{
			lightSet.SpotLights.Remove( light );

			base.Removed( gs, entity );
		}


		public void SetTransform( Matrix transform )
		{
			light.
			light.Position0	=	transform.TranslationVector + transform.Right * tubeLength * 0.5f;
			light.Position1	=	transform.TranslationVector + transform.Left  * tubeLength * 0.5f;
		}
	}
}
