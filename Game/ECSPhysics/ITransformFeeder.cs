using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.ECSPhysics
{
	interface ITransformFeeder
	{
		void FeedTransform( GameState gs );
	}
}
