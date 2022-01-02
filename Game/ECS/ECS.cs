using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Configuration;

namespace IronStar.ECS
{
	[ConfigClass]
	public static class ECS
	{
		[Config]	public static bool TrackEntities { get; set; } = false;
	}
}
