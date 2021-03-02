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
using Fusion.Widgets.Advanced;

namespace IronStar.SFX2
{
	public class DecalComponent : Component
	{
		[AECategory("Decal Image")]
		[AEAtlasImage(@"decals/decals")]
		public string ImageName { get; set; } = "";

		/// <summary>
		/// 
		/// </summary>
		[AECategory("Decal Size")]
		[AESlider(0, 64, 1f, 1/16f)]
		public float Width { get; set;} = 4;

		/// <summary>
		/// 
		/// </summary>
		[AECategory("Decal Size")]
		[AESlider(0, 64, 1f, 1/16f)]
		public float Height { get; set;} = 4;

		/// <summary>
		/// 
		/// </summary>
		[AECategory("Decal Size")]
		[AESlider(0, 16, 1f, 1/16f)]
		public float Depth { get; set;} = 1;

		/// <summary>
		/// Decal emission intensity
		/// </summary>
		[AECategory("Decal Material")]
		public Color EmissionColor { get; set;} = Color.Black;

		/// <summary>
		/// Decal emission intensity
		/// </summary>
		[AECategory("Decal Material")]
		public float EmissionIntensity { get; set;} = 100;

		/// <summary>
		/// Decal base color
		/// </summary>
		[AECategory("Decal Material")]
		public Color BaseColor { get; set;} = new Color(128,128,128,255);

		/// <summary>
		/// Decal roughness
		/// </summary>
		[AECategory("Decal Material")]
		[AESlider(0, 1, 1/4f, 1/128f)]
		public float Roughness { get; set;}= 0.5f;

		/// <summary>
		/// Decal meatllic
		/// </summary>
		[AECategory("Decal Material")]
		[AESlider(0, 1, 1/4f, 1/128f)]
		public float Metallic { get; set;} = 0.5f;

		/// <summary>
		/// Color blend factor [0,1]
		/// </summary>
		[AECategory("Decal Material")]
		[AESlider(0, 1, 1/4f, 1/128f)]
		public float ColorFactor { get; set;} = 1.0f;

		/// <summary>
		/// Roughmess and specular blend factor [0,1]
		/// </summary>
		[AECategory("Decal Material")]
		[AESlider(0, 1, 1/4f, 1/128f)]
		public float SpecularFactor { get; set;} = 1.0f;

		/// <summary>
		/// Normalmap blend factor [-1,1]
		/// </summary>
		[AECategory("Decal Material")]
		[AESlider(0, 1, 1/4f, 1/128f)]
		public float NormalMapFactor { get; set;} = 1.0f;

		/// <summary>
		/// Falloff factor [-1,1]
		/// </summary>
		[AECategory("Decal Material")]
		[AESlider(0, 1, 1/4f, 1/128f)]
		public float FalloffFactor { get; set;} = 0.5f;
	}
}
