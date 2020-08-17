﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class StepComponent : IComponent
	{
		public bool		Jumped;
		public bool		Landed;
		public bool		LeftStep;
		public bool		RightStep;

		public int		Counter;
		public float	StepTimer;
		public float	StepFraction;
		public bool		Traction;

		public Vector3	GroundVelocity;
		public float	FallVelocity;

		public void Load( GameState gs, Stream stream )
		{
		}

		public void Save( GameState gs, Stream stream )
		{
		}
	}
}