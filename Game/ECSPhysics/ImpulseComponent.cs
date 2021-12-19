using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.ECSPhysics
{
	public class ImpulseComponent : IComponent
	{
		public Vector3 Location;
		public Vector3 Impulse;
		public bool Applied = false;

		public ImpulseComponent()
		{
		}

		public ImpulseComponent( Vector3 location, Vector3 impulse )
		{
			this.Location	=	location;
			this.Impulse	=	impulse;
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
