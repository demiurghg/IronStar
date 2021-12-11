using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Graphics.Scenes;

namespace IronStar.ECSPhysics
{
	public class RagdollController
	{
		readonly PhysicsCore physics;

		public RagdollController( PhysicsCore physics, Scene scene )
		{
			this.physics	=	physics;	
		}


		public void Destroy()
		{
			
		}
	}
}
