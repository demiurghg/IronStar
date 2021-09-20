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
	public class StepComponent : Component
	{
		public bool		Jumped;
		public bool		Landed;
		public bool		LeftStep;
		public bool		RightStep;
		public bool		Crouched;
		public bool		Standed;
		public bool		RecoilLight;
		public bool		RecoilHeavy;

		public int		Counter;
		public float	StepTimer;
		public float	StepFraction;
		public bool		HasTraction;
		public bool		IsCrouching;

		public bool		IsWalkingOrRunning { get { return HasTraction && GroundVelocity.Length() > 0.125f; } }

		public Vector3	GroundVelocity;
		public float	FallVelocity;
		public Vector3	LocalAcceleration;
	}
}
