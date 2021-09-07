using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	public class Component : IComponent
	{
		public virtual void Load( GameState gs, Stream stream )
		{
		}

		public virtual void Save( GameState gs, Stream stream )
		{
		}
	}
}
