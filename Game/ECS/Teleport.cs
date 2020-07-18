using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	/// <summary>
	/// Indicates, that given entity has been reaspawned/teleported by gameplay code.
	/// Physics system will override physical bodies' transform based on Transform component
	/// </summary>
	public class Teleport : Component
	{
	}
}
