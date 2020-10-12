﻿using System;
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
		public int	Capacity;
		public int	Count;

		public AmmoComponent() {}
		public AmmoComponent(int count, int capacity) { Count = count; Capacity = capacity; }

		public void Load( GameState gs, Stream stream )	{	}
		public void Save( GameState gs, Stream stream )	{	}
	}
}
