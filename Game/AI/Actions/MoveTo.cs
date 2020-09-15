﻿using System;
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
	public class MoveTo : BTAction
	{
		readonly string keyLocation;
		Vector3[] route;


		public MoveTo( string keyLocation )
		{
			this.keyLocation = keyLocation;
		}


		public override bool Initialize(Entity entity)
		{
			var	navSystem	=	entity.gs.GetService<NavigationSystem>();
			var blackboard	=	entity.GetComponent<BehaviorComponent>().Blackboard;
			var targetPoint	=	Vector3.Zero;
			var originPoint	=	entity.Location;

			if (blackboard.TryGet( keyLocation, out targetPoint ))
			{
				route	=	navSystem.FindRoute( originPoint, targetPoint );
				return route!=null;
			}
			else
			{
				route	=	null;
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

		
		public override BTStatus Update( GameTime gameTime, Entity entity, bool cancel )
		{
			var dr = entity.gs.Game.RenderSystem.RenderWorld.Debug;

			if (cancel)
			{
				return BTStatus.Failure;
			}

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
				uc.Yaw	=	(float)Math.Atan2( -dir.X, -dir.Z );
				uc.MoveForward = 1.0f;
			}

			return routeResult;
		}
	}
}
