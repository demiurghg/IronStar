﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.BTCore;
using IronStar.ECS;

namespace IronStar.AI
{
	public class BehaviorComponent : IComponent
	{
		public float VisibilityFov		=	45.0f;
		public float VisibilityRange	=	450.0f;
		public float HearingRange		=	15.0f;

		public Entity LastSeenTarget	=	null;


		public readonly Blackboard Blackboard;

		public BehaviorComponent()
		{
			Blackboard	=	new Blackboard();
		}
		
		public void Load( GameState gs, Stream stream )
		{
		}

		public void Save( GameState gs, Stream stream )
		{
		}
	}
}
