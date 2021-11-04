using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.Gameplay.Weaponry;
using System.Collections.Concurrent;

namespace IronStar.Gameplay
{
	public class UserCommandQueue : ConcurrentQueue<UserCommand>
	{
	}
}
