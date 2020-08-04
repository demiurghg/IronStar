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
		public float OuterRadius 
		{ 
			get { return light.RadiusOuter; }
			set { light.RadiusOuter = value; }
		}
		
		[AECategory("Spot-light")]
		[AEValueRange(0, 8, 1, 0.125f)]
		public float TubeRadius 
		{ 
			get { return light.RadiusInner; }
			set { light.RadiusInner = value; }
		}

		[AECategory("Spot-light")]
		[AEValueRange(0, 32, 1, 0.125f)]
		public float TubeLength { get; set; } = 0.0f;

		[AECategory("Light Color")]
		[AEDisplayName("Light Color")]
		public Color LightColor
		{ 
			get { return lightColor; }
			set 
			{ 
				lightColor	= value; 
				light.Intensity = lightColor.ToColor4() * MathUtil.Exp2( lightIntensity ); 
			}
		}

		[AECategory("Light Color")]
		[AEDisplayName("Intensity")]
		[AEValueRange(0, 12, 10, 1)]
		public float LightIntensity 
		{ 
			get { return lightIntensity; }
			set 
			{ 
				lightIntensity	= value; 
				light.Intensity = lightColor.ToColor4() * MathUtil.Exp2( lightIntensity ); 
			}
		}

		[AECategory("Global Illumination")]
		[AEDisplayName("Enable GI")]
		public bool EnableGI 
		{ 
			get { return light.EnableGI; }
			set { light.EnableGI = value; }
		}

		[AECategory("Spot Shadow")]
		[AEDisplayName("Spot Mask")]
		[AEAtlasImage("spots/spots")]
		public string SpotMaskName
		{ 
			get { return light.SpotMaskName; }
			set { light.SpotMaskName = value; }
		}
		
		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow LOD Bias")]
		[AEValueRange(0, 8, 1, 1)]
		public int LodBias
		{ 
			get { return light.LodBias; }
			set { light.LodBias = value; }
		}
		
		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow Depth Bias")]
		[AEValueRange(0, 1/512f, 1/8192f, 1/16384f)]
		public float DepthBias
		{ 
			get { return light.DepthBias; }
			set { light.DepthBias = value; }
		}

		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow Slope Bias")]
		[AEValueRange(0, 8, 1, 0.125f/4.0f)]
		public float SlopeBias
		{ 
			get { return light.SlopeBias; }
			set { light.SlopeBias = value; }
		}

		float nearPlane = 0.5f;
		float farPlane = 15;
		float fovVertical = 60;
		float fovHorizontal = 60;

		[AECategory("Spot Shape")]
		[AEValueRange(0, 4, 1/4f, 1/64f)]
		public float NearPlane
		{
			get { return nearPlane; }
			set { nearPlane = value; UpdateSpotMatrix(); }
		}
		
		[AECategory("Spot Shape")]
		[AEValueRange(0, 100, 1, 1/8f)]
		public float FarPlane
		{
			get { return farPlane; }
			set { farPlane = value; UpdateSpotMatrix(); }
		}
		
		[AECategory("Spot Shape")]
		[AEValueRange(0, 150, 15, 1)]
		public float FovVertical
		{
			get { return fovVertical; }
			set { fovVertical = value; UpdateSpotMatrix(); }
		}
		
		[AECategory("Spot Shape")]
		[AEValueRange(0, 150, 15, 1)]
		public float FovHorizontal
		{
			get { return fovHorizontal; }
			set { fovHorizontal = value; UpdateSpotMatrix(); }
		}

		void UpdateSpotMatrix()
		{
			float n	=	NearPlane;
			float f	=	FarPlane;
			float w	=	(float)Math.Tan( MathUtil.DegreesToRadians( FovHorizontal/2 ) ) * NearPlane * 2;
			float h	=	(float)Math.Tan( MathUtil.DegreesToRadians( FovVertical/2	) ) * NearPlane * 2;

			light.Projection	=	Matrix.PerspectiveRH( w, h, n, f );
		}

		

		Color lightColor = Color.White;
		float lightIntensity = 8;

		RSSpotLight light;
		LightSet lightSet;


		public SpotLight ()
		{
			light	=	new RSSpotLight();
			light.RadiusOuter	=	15;
			light.RadiusInner	=	0.25f;
		}


		public override void Added( GameState gs, Entity entity )
		{
			base.Added( gs, entity );

			lightSet	=	gs.GetService<RenderSystem>().RenderWorld.LightSet;
			lightSet.SpotLights.Add ( light );
		}


		public override void Removed( GameState gs )
		{
			lightSet.SpotLights.Remove ( light );
			base.Removed( gs );
		}


		public void SetTransform( Matrix transform )
		{
			light.Position0	=	transform.TranslationVector + transform.Right * TubeLength * 0.5f;
			light.Position1	=	transform.TranslationVector + transform.Left  * TubeLength * 0.5f;

			light.SpotView	=	Matrix.Invert( transform );
		}
	}
}
