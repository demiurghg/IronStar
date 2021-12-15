using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.ECSPhysics
{
	public class RagdollComponent : IComponent
	{
		public float Scale;

		public RagdollComponent()
		{
		}

		public RagdollComponent(float scale)
		{
			this.Scale	=	scale;
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}

		public void Save( GameState gs, BinaryWriter writer )
		{
			
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			
		}
	}
}
