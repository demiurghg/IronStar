using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	public abstract class EntityAction
	{
		public abstract void Execute( GameState gs, Entity target );
	}


	public class EntityActionAttribute : Attribute
	{
		public readonly string ActionName;

		public EntityActionAttribute( string actionName )
		{
			ActionName = actionName;
		}
	}
}
