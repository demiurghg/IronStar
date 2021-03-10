using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.SFX;
using Fusion.Development;
using System.Drawing.Design;
using Fusion.Core.Shell;
using IronStar.ECS;
using IronStar.SFX2;
using Fusion.Widgets.Advanced;

namespace IronStar.Mapping {
	public class MapDecal : MapNode 
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
		[AESlider(0, 1.0f, 1/4f, 1/128f)]
		public float FalloffFactor { get; set;} = 0.5f;
		

		Decal decal = null;


		/// <summary>
		/// 
		/// </summary>
		public MapDecal ()
		{
		}


		public override void SpawnNodeECS( GameState gs )
		{
			ecsEntity = gs.Spawn();

			ecsEntity.AddComponent( new Transform( TranslateVector, RotateQuaternion, 1 ) );
			ecsEntity.AddComponent( CreateDecal() );
			
			base.SpawnNodeECS( gs );
		}


		DecalComponent CreateDecal()
		{
			var dc = new DecalComponent();

			dc.ImageName			=	ImageName			;
			dc.Width				=	Width				;
			dc.Height				=	Height				;
			dc.Depth				=	Depth				;
			dc.EmissionColor		=	EmissionColor		;
			dc.EmissionIntensity	=	EmissionIntensity	;
			dc.BaseColor			=	BaseColor			;
			dc.Roughness			=	Roughness			;
			dc.Metallic				=	Metallic			;
			dc.ColorFactor			=	ColorFactor			;
			dc.SpecularFactor		=	SpecularFactor		;
			dc.NormalMapFactor		=	NormalMapFactor		;
			dc.FalloffFactor		=	FalloffFactor		;				   

			return dc;
		}

		public override MapNode DuplicateNode()
		{
			var newNode = (MapDecal)MemberwiseClone();
			newNode.decal = null;
			newNode.Name = Guid.NewGuid().ToString();
			return newNode;
		}

		public override BoundingBox GetBoundingBox()
		{
			return new BoundingBox( Width, Height, 0.25f );
		}
	}
}
