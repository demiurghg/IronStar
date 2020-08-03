﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public enum PowerUpType 
	{
		Health,
		Armor,
	}

	public class PowerupComponent : IComponent
	{
		public PowerUpType Type;
		public int Amount;

		public void Added( GameState gs, Entity entity ) {}
		public void Removed( GameState gs ) {}
		public void Load( GameState gs, Stream stream ) {}
		public void Save( GameState gs, Stream stream ) {}
	}
}