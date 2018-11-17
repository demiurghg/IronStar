using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.AI {
	public class NavigationSettings {

		public float WalkableHeight = 2.0f;
		public float WalkableRadius = 0.5f;
		public float WalkableClimb  = 0.5f;
		public float WalkableAngle  = (float)(Math.PI/4.0f);

		public float VoxelStep		= 0.5f;

	}
}
