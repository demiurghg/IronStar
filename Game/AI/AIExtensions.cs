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

			if (controller!=null) 
			{
				return transform.Position + controller.PovOffset;
			} 
			else
			{
				return transform.Position;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="attcker">Attacker entity.</param>
		/// <param name="target">Target entity. Could be null, in such case always return false.</param>
		/// <param name="maxDistance">Max distance. If distance between targets more than max distance target is considered hidden</param>
		/// <returns></returns>
		public static bool HasLineOfSight( this Entity attacker, Entity target, float maxDistance = float.MaxValue )
		{
			if (target==null) 
			{
				return false;
			}

			try
			{
				var from	=	attacker.GetPOV();
				var to		=	target.GetPOV();

				if (Vector3.Distance(from,to) > maxDistance)
				{
					return false;
				}
				else
				{
					return attacker.gs.GetService<PhysicsCore>().HasLineOfSight( from, to, attacker, target );
				}
			} 
			catch (InvalidOperationException)
			{
				return false;
			}
		}
	}
}
