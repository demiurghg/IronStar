using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using Fusion.Engine.Graphics;
using RSOmniLight = Fusion.Engine.Graphics.OmniLight;
using Fusion.Core.Shell;
using System.IO;
using Fusion.Widgets.Advanced;

namespace IronStar.SFX2
{
	public class LightProbeSphere : IComponent
	{
		public string name;

		[AECategory("Light probe")]
		[AESlider(0,256,8,0.25f)]
		public float Radius { get; set; } = 32;

		[AECategory("Light probe")]
		[AEDisplayName("Transition Width")]
		[AESlider(0.25f,64,1,0.25f)]
		public float Transition  { get; set; } = 8f;

		public LightProbeSphere ( string name )
		{
			this.name	=	name;
		}


		public LightProbeSphere () : this( Guid.NewGuid().ToString() )
		{
		}

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( name );
			writer.Write( Radius );
			writer.Write( Transition );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			name		=	reader.ReadString();
			Radius		=	reader.ReadSingle();
			Transition	=	reader.ReadSingle();
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
