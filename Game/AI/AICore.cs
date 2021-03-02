using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Configuration;
using Fusion.Widgets.Advanced;

namespace IronStar.AI
{
	public class AICore : GameComponent
	{
		[Config]
		[AECategory("Debugging")]
		public bool ShowNavigationMesh { get; set; }

		public AICore( Game game ) : base( game )
		{
		}
	}
}
