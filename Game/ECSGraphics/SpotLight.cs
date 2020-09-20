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
	public class SpotLight : Component
	{
		[AECategory("Spot-light")]
		[AEValueRange(0, 100, 1, 0.125f)]
		public float OuterRadius { get; set; } = 15.0f;
		
		[AECategory("Spot-light")]
		[AEValueRange(0, 8, 1, 0.125f)]
		public float TubeRadius { get; set; } = 0.5f;

		[AECategory("Spot-light")]
		[AEValueRange(0, 32, 1, 0.125f)]
		public float TubeLength { get; set; } = 0.0f;

		[AECategory("Light Color")]
		[AEDisplayName("Light Color")]
		public Color LightColor { get; set; }

		[AECategory("Light Color")]
		[AEDisplayName("Intensity")]
		[AEValueRange(0, 12, 10, 1)]
		public float LightIntensity { get; set; }

		[AECategory("Global Illumination")]
		[AEDisplayName("Enable GI")]
		public bool EnableGI { get; set; }

		[AECategory("Spot Shadow")]
		[AEDisplayName("Spot Mask")]
		[AEAtlasImage("spots/spots")]
		public string SpotMaskName { get; set; }
		
		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow LOD Bias")]
		[AEValueRange(0, 8, 1, 1)]
		public int LodBias { get; set; }
		
		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow Depth Bias")]
		[AEValueRange(0, 1/512f, 1/8192f, 1/16384f)]
		public float DepthBias { get; set; }

		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow Slope Bias")]
		[AEValueRange(0, 8, 1, 0.125f/4.0f)]
		public float SlopeBias { get; set; }

		float nearPlane = 0.5f;
		float farPlane = 15;
		float fovVertical = 60;
		float fovHorizontal = 60;

		[AECategory("Spot Shape")]
		[AEValueRange(0, 4, 1/4f, 1/64f)]
		public float NearPlane { get; set; } = 0.5f;
		
		[AECategory("Spot Shape")]
		[AEValueRange(0, 100, 1, 1/8f)]
		public float FarPlane { get; set; } = 15.0f;
		
		[AECategory("Spot Shape")]
		[AEValueRange(0, 150, 15, 1)]
		public float FovVertical { get; set; } = 60.0f;
		
		[AECategory("Spot Shape")]
		[AEValueRange(0, 150, 15, 1)]
		public float FovHorizontal { get; set; } = 60.0f;

		public Matrix ComputeSpotMatrix()
		{
			float n	=	NearPlane;
			float f	=	FarPlane;
			float w	=	(float)Math.Tan( MathUtil.DegreesToRadians( FovHorizontal/2 ) ) * NearPlane * 2;
			float h	=	(float)Math.Tan( MathUtil.DegreesToRadians( FovVertical/2	) ) * NearPlane * 2;

			return	Matrix.PerspectiveRH( w, h, n, f );
		}
	}
}
