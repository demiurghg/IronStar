using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	/// <summary>
	/// #TODO #ECS -- implement Clone and Interpolate using reflection and IL generator.
	/// https://stackoverflow.com/questions/966451/fastest-way-to-do-shallow-copy-in-c-sharp
	/// </summary>
	public abstract class Component : IComponent
	{
		public virtual void Save( GameState gs, BinaryWriter writer ) {}

		public virtual void Load( GameState gs, BinaryReader reader ) {}

		public virtual IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public virtual IComponent Interpolate( IComponent previuous, float factor )
		{
			return Clone();
		}
	}
}
