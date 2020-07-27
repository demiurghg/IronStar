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

		public void Added( GameState gs, Entity entity ) {	}
		public void Removed( GameState gs )	{	}
		public void Load( GameState gs, Stream stream )	{	}
		public void Save( GameState gs, Stream stream )	{	}
	}
}
