using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class KeyComponent : IComponent
	{
		public string	Name;

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/


		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( Name );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Name	=	reader.ReadString();
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}
	}
}
