using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.BTCore;
using IronStar.ECS;
using IronStar.ECSFactories;
using IronStar.ECSPhysics;

namespace IronStar.AI
{
	public static class AIExtensions
	{
		public static Blackboard GetBlackboard( this Entity entity )
		{
			var bb = entity.GetComponent<BehaviorComponent>()?.Blackboard;
			
			if (bb==null) 
			{
				throw new InvalidOperationException("Entity has no behavior component and blackboard cannot be retrieved");
			}

			return bb;
		}


		public static Entity[] GetPlayers( this GameState gs )
		{
			return gs.QueryEntities( PlayerFactory.PlayerAspect ).ToArray();
		}


		public static Entity GetPlayer( this GameState gs )
		{
			return gs.QueryEntities( PlayerFactory.PlayerAspect ).LastOrDefault();
		}


		public static Vector3 GetLocation( this Entity entity )
		{
			var transform = entity.GetComponent<Transform>();
			
			if (transform==null) 
			{
				throw new InvalidOperationException("Entity has no " + nameof(Transform) + " component");
			}

			return transform.Position;
		}


		public static Vector3 GetPOV( this Entity entity )
		{
			var transform	=	entity.GetComponent<Transform>();
			var controller	=	entity.GetComponent<CharacterController>();
			
			if (transform==null) 
			{
				throw new InvalidOperationException("Entity has no " + nameof(Transform) + " component");
			}

			if (controller==null) 
			{
				throw new InvalidOperationException("Entity has no " + nameof(CharacterController) + " component");
			}

			return transform.Position + controller.PovOffset;
		}
	}
}
