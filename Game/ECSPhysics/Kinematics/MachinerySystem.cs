using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.ECSPhysics.Kinematics
{
	public class MachinerySystem : KinematicSystem<MachineryComponent>
	{
		public MachinerySystem( PhysicsCore physics ) : base( physics )
		{
		}
	}
}
