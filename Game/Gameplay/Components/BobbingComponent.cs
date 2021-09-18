using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	// #TODO #PLAYER -- separate bobbing ans user command component
	public class BobbingComponent : IComponent
	{
		public float BobYaw;
		public float BobPitch;
		public float BobRoll;
		public float BobUp;

		public void Load( GameState gs, Stream stream )
		{
		}

		public void Save( GameState gs, Stream stream )
		{
		}
	}
}
