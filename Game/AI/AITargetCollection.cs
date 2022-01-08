using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.AI
{
	public class AITargetCollection : List<AITarget>
	{
		public bool HasEntity( Entity entity )
		{
			foreach ( var target in this )
			{
				if (target.Entity==entity)
				{
					return true;
				}
			}

			return false;
		}


		public int EraseEntity( Entity entity )
		{
			return RemoveAll( target => target.Entity==entity );
		}
	}
}
