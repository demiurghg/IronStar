using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.ECSPhysics
{
	interface IMotionState
	{
		Vector3		Position { get; set; }
		Quaternion	Rotation { get; set; }
		Vector3		LinearVelocity { get; set; }
		Vector3		AngularVelocity { get; set; }
	}
}
