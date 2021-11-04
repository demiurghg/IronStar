using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.ECSPhysics
{
	public class GravityComponent : IComponent
	{
		public float Magnitude;

		public GravityComponent() : this(48)
		{
		}

		public GravityComponent( float g )
		{
			Magnitude	=	g;
		}

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( Magnitude );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Magnitude	=	reader.ReadSingle();
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
