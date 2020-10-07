using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class AmmoComponent : IComponent
	{
		public string Name;
		public int	Capacity;
		public int	Count;

		public AmmoComponent() {}
		public AmmoComponent(string name, int count, int capacity) { Name = name; Count = count; Capacity = capacity; }

		public void Load( GameState gs, Stream stream )	{	}
		public void Save( GameState gs, Stream stream )	{	}
	}
}
