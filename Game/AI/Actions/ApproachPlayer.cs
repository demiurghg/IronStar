using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using IronStar.BTCore;
using IronStar.ECS;
using IronStar.ECSFactories;
using IronStar.Gameplay;

namespace IronStar.AI.Actions
{
	public class ApproachPlayer : BTAction
	{
		Vector3 targetPoint;
		Vector3 originPoint;
		NavigationSystem navSystem;
		int frames;

		Vector3[] route;

		public override bool Initialize(Entity entity)
		{
			navSystem			=	entity.gs.GetService<NavigationSystem>();
			var playerEntity	=	entity.gs.QueryEntities( PlayerFactory.PlayerAspect ).LastOrDefault();

			if (playerEntity!=null)
			{
				targetPoint	=	playerEntity.GetComponent<Transform>().Position;	
				originPoint	=	entity.GetComponent<Transform>().Position;

				route		=	navSystem.FindRoute( originPoint, targetPoint );
				frames		=	0;

				return route!=null;
			}
			else
			{
				return false;
			}
		}

		
		public override void Terminate( Entity entity, BTStatus status )
		{
			var uc = entity.GetComponent<UserCommandComponent>();

			if (uc!=null)
			{
				uc.MoveForward = 0;
			}
		}

		
		public override BTStatus Update( GameTime gameTime, Entity entity )
		{
			frames++;

			var dr = entity.gs.Game.RenderSystem.RenderWorld.Debug;

			for (int i=0; i<route.Length-1; i++)
			{
				var p0 = route[i];
				var p1 = route[i+1];
				dr.DrawLine( p0, p1, Color.Red, Color.Red, 5, 5 );
			}

			var originPoint	=	entity.GetComponent<Transform>().Position;
			var targetPoint =	Vector3.Zero;

			var routeResult	=	NavigationRouter.FollowRoute( route, originPoint, 10, 7, 3, out targetPoint );

			dr.DrawLine( originPoint, targetPoint, Color.Orange, Color.Orange, 10, 1 );

			var uc = entity.GetComponent<UserCommandComponent>();

			if (uc!=null)
			{
				var dir	=	targetPoint - originPoint;
				// #TODO #AI #MONSTER -- flip view model
				uc.Yaw	=	(float)Math.Atan2( dir.X, dir.Z );
				uc.MoveForward = -1;
			}

			return routeResult;
		}
	}
}
