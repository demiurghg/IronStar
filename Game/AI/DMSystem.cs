using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.Gameplay;
using IronStar.Gameplay.Components;

namespace IronStar.AI
{
	class DMSystem : StatelessSystem<AIComponent>
	{
		readonly NavSystem nav;
		readonly Aspect aiAspect;
		readonly AIConfig defaultConfig = new AIConfig();

		public DMSystem( NavSystem nav )
		{
			this.nav	=	nav;

			aiAspect	=	new Aspect()
						.Include<AIComponent,Transform>()
						;
		}


		protected override void Process( Entity entity, GameTime gameTime, AIComponent ai )
		{
			var cfg = defaultConfig;
			var dt  = gameTime.ElapsedSec;

			if (ai.ThinkTimer.IsElapsed)
			{
				InvokeNode( entity, ai, cfg );

				ai.ThinkTimer.SetND( cfg.ThinkTime );
			}

			ai.ThinkTimer.Update( gameTime );
			ai.IdleTimer.Update( gameTime );

			Move( dt, entity, ai, cfg );
		}

		bool InvokeNode( Entity e, AIComponent ai, AIConfig cfg )
		{
			switch (ai.DMNode)
			{
				case DMNode.Idle:		return Idle( e, ai, cfg );
				case DMNode.Roaming:	return Roaming( e, ai, cfg );
			}

			return true;
		}

		void Enter( AIComponent ai, DMNode newNode, string reason = "" )
		{
			var oldNode	=	ai.DMNode;
			ai.DMNode	=	newNode;

			Log.Debug("{0} -> {1} : {2}", oldNode, newNode, reason );
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Routing :
		-----------------------------------------------------------------------------------------------*/

		void Move( float dt, Entity e, AIComponent ai, AIConfig cfg )
		{
			var uc		= e.GetComponent<UserCommandComponent>();
			var t		= e.GetComponent<Transform>();
			var dr		= e.gs.Game.RenderSystem.RenderWorld.Debug.Async;
			var route	= ai.ActiveRoute;

			if (uc==null)
			{
				return;
			}

			if (ai.ActiveRoute!=null)
			{
				var origin	=	t.Position;
				var target	=	Vector3.Zero;
				var factor	=	0f;
				var status	=	NavRouter.FollowRoute( route, origin, 10, 7, 3, out target, out factor );
			
				if (status==BTCore.BTStatus.Failure) ai.Goal.Failed = true;
				if (status==BTCore.BTStatus.Success) ai.Goal.Succeed = true;

				var rate	=	dt * cfg.RotationRate;

				uc.RotateTo( origin, target, rate, 0 );
				uc.Move = factor;

				dr.DrawPoint( origin, 1, Color.Black, 3 );
				dr.DrawLine( origin, target, Color.Black, Color.Black, 5, 1 );

				for (int i=0; i<route.Length-1; i++)
				{
					dr.DrawLine( route[i], route[i+1], Color.Black, Color.Black, 2, 2 );
				}
			}
			else
			{
				uc.Move	=	0;
			}
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Decision making nodes :
		-----------------------------------------------------------------------------------------------*/

		bool Idle( Entity e, AIComponent ai, AIConfig cfg )
		{
			var transform	=	e.GetComponent<Transform>();

			if (ai.IdleTimer.IsElapsed)
			{
				var location	=	nav.GetReachablePointInRadius( transform.Position, cfg.RoamRadius );
				var route		=	nav.FindRoute( transform.Position, location );
				var timeout		=	20000;

				ai.Goal			=	new AIGoal( location, timeout );
				ai.ActiveRoute	=	route;

				Enter( ai, DMNode.Roaming, "idle timeout");
			}

			return true;
		}


		bool Roaming( Entity e, AIComponent ai, AIConfig cfg )
		{
			if (ai.Goal.Failed || ai.Goal.Succeed)
			{
				ai.IdleTimer	=	new Timer(cfg.IdleTimeout);
				ai.ActiveRoute	=	null;
				Enter( ai, DMNode.Idle, ai.Goal.Succeed ? "roam point reached" : "failed to reach roam point" );
			}

			return true;
		}
	}
}
