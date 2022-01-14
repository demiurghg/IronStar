using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Configuration;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.ECSPhysics;
using System.Collections.Concurrent;
using Fusion.Core;
using Wintellect.PowerCollections;
using Fusion.Core.Extensions;

namespace IronStar.AI
{
	[ConfigClass]
	public class EQSystem
	{
		readonly NavSystem		nav;
		readonly PhysicsCore	physics;
		readonly ConcurrentQueue<EQPoint>	queue;

		[Config] public static int		MaxIssueInterations	{ get; set; } = 250;
		[Config] public static int		MaxEQPerFrame		{ get; set; } = 5;
		[Config] public static bool		ShowEQPoints		{ get; set; } = false;
		[Config] public static int		QueryTimeout		{ get; set; } = 1500;
		[Config] public static float	QueryCount			{ get; set; } = 50;
		[Config] public static float	QueryRange			{ get; set; } = 50;
		[Config] public static float	MinInterval			{ get; set; } = 3;

		public EQSystem( NavSystem nav, PhysicsCore physics )
		{
			this.nav		=	nav;
			this.physics	=	physics;
			queue			=	new ConcurrentQueue<EQPoint>();
		}


		public void RequestPoints( Entity owner, AITarget target )
		{
			Vector3 origin, location, pov;
			AIComponent ai;

			ai = owner.GetComponent<AIComponent>();
			
			if (AIUtils.IsAlive(owner) && target!=null && ai!=null && owner.TryGetLocation(out origin) && owner.TryGetPOV(out pov, 0.5f))
			{
				int count = 0;
				var povDelta = pov - origin;

				for (int i=0; i<MaxIssueInterations; i++)
				{
					if (nav.TryGetReachablePointInRadius( origin, QueryRange, out location ))
					{
						var minDistance = float.MaxValue;
						foreach (var cp in ai.CombatPoints)
						{
							minDistance = Math.Min( minDistance, Vector3.Distance( cp.Location, location ));
						}

						if (minDistance > MinInterval)
						{
							var point	=	new EQPoint(owner, ai, location, location + povDelta, target, QueryTimeout);
							ai.CombatPoints.Add( point );
							queue.Enqueue( point );
							count++;
						}
					}

					if (count>QueryCount) 
					{
						break;
					}
				}
			}
		}


		public void RefreshPoints( Entity owner, AITarget target, float threshold = 0.2f )
		{
			/*Vector3 origin, location, pov;
			AIComponent ai;

			ai = owner.GetComponent<AIComponent>();
			
			if (ai!=null && owner.TryGetLocation(out origin) && owner.TryGetPOV(out pov))
			{
				foreach (var p in ai.CombatPoints)
				{
					if (p.Timer.Fraction>threshold)
					{
						queue.Enqueue(p);
					}
				}
			} */
		}


		public void ScanEnvironment( GameTime gameTime )
		{
			for (int i=0; i<MaxEQPerFrame; i++)
			{
				EQPoint point;

				if ( queue.TryDequeue( out point ) )
				{
					CheckPoint( point );
				}
				else
				{
					break;
				}
			}
		}


		public void TickEQPoints( GameTime gameTime, Entity e, AIComponent ai )
		{
			//	draw :
			if (ShowEQPoints)
			{
				var dr = e.gs.Game.RenderSystem.RenderWorld.Debug.Async;

				foreach ( var point in ai.CombatPoints )
				{
					var color = Color.Black;
					switch (point.State)
					{
						case EQState.NotReady:	color = Color.Black	; break;
						case EQState.Exposed:	color = Color.Red	; break;
						case EQState.Protected:	color = Color.Lime	; break;
					}

					dr.DrawWaypoint( point.Location, 0.7f, color, 1 );
				}
			}

			//	update timers :
			foreach ( var combatPoint in ai.CombatPoints )
			{
				combatPoint.Timer.Update( gameTime );
			}
			
			ai.CombatPoints.RemoveAll( cp => cp.Timer.IsElapsed );
		}


		public bool TryGetPoint( Entity e, AIComponent ai, EQState state, Func<float,float> distFunc, out Vector3 location )
		{
			Vector3 origin;
			location = Vector3.Zero;
			
			if (e.TryGetLocation(out origin))
			{
				var bestCP =	ai.CombatPoints
								.Where( p1 => p1.State == state )
								.SelectMinOrDefault( p2 => distFunc(Vector3.Distance(p2.Location, origin)) );

				if (bestCP!=null)
				{
					location = bestCP.Location;
					return true;
				}
			}
			
			return false;
		}


		void CheckPoint( EQPoint point )
		{
			var target = point.Target?.Entity;
			var pov = Vector3.Zero;

			if (target!=null && target.TryGetPOV(out pov))
			{
				if (physics.HasLineOfSight(pov, point.POV, point.Owner, target))
				{
					point.State	=	EQState.Exposed;	
				}
				else
				{
					point.State	=	EQState.Protected;	
				}
			}
		}
	}
}
