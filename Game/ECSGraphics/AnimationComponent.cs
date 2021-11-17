using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;
using Fusion.Core.Mathematics;

namespace IronStar.ECSGraphics
{
	public class AnimationComponent : IComponent
	{
		public float Frame = 0;

		public AnimationComponent()
		{
		}

		public AnimationComponent( float frame )
		{
			Frame = frame;
		}

		public IComponent Clone()
		{
			return (IComponent)this.MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			var frame = MathUtil.Lerp( ((AnimationComponent)previous).Frame, Frame, factor );
			return new AnimationComponent( frame );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Frame	=	reader.ReadSingle();
		}

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( Frame );
		}
	}
}
