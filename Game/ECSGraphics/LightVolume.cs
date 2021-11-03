using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using Fusion.Engine.Graphics;
using Fusion.Core.Shell;
using Fusion.Widgets.Advanced;
using System.IO;

namespace IronStar.SFX2
{
	[Obsolete]
	public class LightVolume : IComponent
	{
		[AECategory("Light Volume")]
		[AESlider(4, 256, 4, 1)]
		public int ResolutionX 
		{ 
			get; set; 
		} = 16;
		
		[AECategory("Light Volume")]
		[AESlider(4, 256, 4, 1)]
		public int ResolutionY 
		{ 
			get; set; 
		} = 16;
		
		[AECategory("Light Volume")]
		[AESlider(4, 256, 4, 1)]
		public int ResolutionZ 
		{ 
			get; set; 
		} = 16;

		[AECategory("Light Volume")]
		[AESlider(4, 1024, 32, 4)]
		public float Width 
		{ 
			get; set; 
		}
		
		[AECategory("Light Volume")]
		[AESlider(4, 1024, 32, 4)]
		public float Height 
		{ 
			get; set; 
		}
		
		[AECategory("Light Volume")]
		[AESlider(4, 1024, 32, 4)]
		public float Depth 
		{ 
			get; set; 
		}

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( ResolutionX	);
			writer.Write( ResolutionY	);
			writer.Write( ResolutionZ	);
			writer.Write( Width 		);
			writer.Write( Height 		);
			writer.Write( Depth 		);
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			ResolutionX	=	reader.ReadInt32();
			ResolutionY	=	reader.ReadInt32();
			ResolutionZ	=	reader.ReadInt32();
			Width 		=	reader.ReadSingle();
			Height 		=	reader.ReadSingle();
			Depth 		=	reader.ReadSingle();
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
