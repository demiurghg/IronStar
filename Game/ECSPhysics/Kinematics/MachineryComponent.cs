using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.ECSPhysics.Kinematics
{
	public class MachineryComponent : IComponent
	{
		public TimeSpan Time;

		public IComponent Clone()
		{
			return (IComponent)this.MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}

		public void Load( GameState gs, BinaryReader reader )
		{
		}

		public void Save( GameState gs, BinaryWriter writer )
		{
		}
	}
}
