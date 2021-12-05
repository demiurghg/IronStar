using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.Environment
{
	public enum UIEffect
	{
		None,
		Glitches,
	}

	public class GUIComponent : IComponent
	{
		public bool Interactive = false;

		public GUIComponent()
		{
		}

		public GUIComponent(bool interacrtive)
		{
			this.Interactive	=	interacrtive;
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
			writer.Write( Interactive );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Interactive = reader.ReadBoolean();
		}
	}
}
