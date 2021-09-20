using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class NameComponent : Component
	{
		public string	Name;

		public NameComponent()
		{
			Name = "";
		}

		public NameComponent(string name)
		{
			Name = name;
		}
	}
}
