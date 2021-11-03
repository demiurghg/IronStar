using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using IronStar.ECS;
using Fusion.Engine.Graphics;
using RSSpotLight = Fusion.Engine.Graphics.SpotLight;
using Fusion.Core.Shell;
using Fusion.Widgets.Advanced;
using System.IO;

namespace IronStar.SFX2
{
	public class SpotLight : IComponent
	{
		[AECategory("Spot-light")]
		[AESlider(0, 100, 1, 0.125f)]
		public float OuterRadius { get; set; } = 15.0f;
		
		[AECategory("Spot-light")]
		[AESlider(0, 8, 1, 0.125f)]
		public float TubeRadius { get; set; } = 0.5f;

		[AECategory("Spot-light")]
		[AESlider(0, 32, 1, 0.125f)]
		public float TubeLength { get; set; } = 0.0f;

		[AECategory("Light Color")]
		[AEDisplayName("Light Color")]
		public Color LightColor { get; set; }

		[AECategory("Light Color")]
		[AEDisplayName("Intensity")]
		[AESlider(0, 12, 10, 1)]
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
		[AESlider(0, 8, 1, 1)]
		public int LodBias { get; set; }
		
		[AECategory("Spot Shape")]
		[AESlider(0, 4, 1/4f, 1/64f)]
		public float NearPlane { get; set; } = 0.5f;
		
		[AECategory("Spot Shape")]
		[AESlider(0, 100, 1, 1/8f)]
		public float FarPlane { get; set; } = 15.0f;
		
		[AECategory("Spot Shape")]
		[AESlider(0, 150, 15, 1)]
		public float FovVertical { get; set; } = 60.0f;
		
		[AECategory("Spot Shape")]
		[AESlider(0, 150, 15, 1)]
		public float FovHorizontal { get; set; } = 60.0f;

		public Matrix ComputeSpotMatrix()
		{
			float n	=	NearPlane;
			float f	=	FarPlane;
			float w	=	(float)Math.Tan( MathUtil.DegreesToRadians( FovHorizontal/2 ) ) * NearPlane * 2;
			float h	=	(float)Math.Tan( MathUtil.DegreesToRadians( FovVertical/2	) ) * NearPlane * 2;

			return	Matrix.PerspectiveRH( w, h, n, f );
		}

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( OuterRadius	 );	
			writer.Write( TubeRadius	 );	
			writer.Write( TubeLength	 );	
			writer.Write( LightColor	 );	
			writer.Write( LightIntensity );	
			writer.Write( EnableGI		 );
			writer.Write( SpotMaskName	 );
			writer.Write( LodBias		 );	
			writer.Write( NearPlane		 );
			writer.Write( FarPlane		 );
			writer.Write( FovVertical	 );	
			writer.Write( FovHorizontal	 );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			OuterRadius		=	reader.ReadSingle();
			TubeRadius		=	reader.ReadSingle();
			TubeLength		=	reader.ReadSingle();
			LightColor		=	reader.Read<Color>();
			LightIntensity	=	reader.ReadSingle();
			EnableGI		=	reader.ReadBoolean();
			SpotMaskName	=	reader.ReadString();
			LodBias			=	reader.ReadInt32();
			NearPlane		=	reader.ReadSingle();
			FarPlane		=	reader.ReadSingle();
			FovVertical		=	reader.ReadSingle();
			FovHorizontal	=	reader.ReadSingle();
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float factor )
		{
			return Clone();
		}
	}
}
