using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using IronStar.ECS;
using Fusion.Core.Shell;
using Fusion.Widgets.Advanced;
using System.IO;

namespace IronStar.SFX2
{
	public class OmniLight : IComponent
	{
		[AECategory("Omni-light")]
		[AESlider(0, 100, 1, 0.125f)]
		public float OuterRadius 
		{ 
			get; set; 
		}
		
		[AECategory("Omni-light")]
		[AESlider(0, 8, 1, 0.125f)]
		public float TubeRadius 
		{ 
			get; set; 
		}

		[AECategory("Omni-light")]
		[AESlider(0, 32, 1, 0.125f)]
		public float TubeLength { get; set; } = 0.0f;

		[AECategory("Light Color")]
		[AEDisplayName("Light Color")]
		public Color LightColor
		{ 
			get; set; 
		}

		[AECategory("Light Color")]
		[AEDisplayName("Intensity")]
		[AESlider(0, 12, 10, 1)]
		public float LightIntensity 
		{ 
			get; set; 
		}
		

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( OuterRadius		);
			writer.Write( TubeRadius		);
			writer.Write( TubeLength		);
			writer.Write( LightColor		);
			writer.Write( LightIntensity	);
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			OuterRadius		=	reader.ReadSingle(); 
			TubeRadius		=	reader.ReadSingle();
			TubeLength		=	reader.ReadSingle();
			LightColor		=	reader.Read<Color>();
			LightIntensity	=	reader.ReadSingle();
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
