using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using IronStar.ECS;
using IronStar.Gameplay.Components;

namespace IronStar.Gameplay.Systems
{
	public class TriggerSystem : ISystem
	{
		Aspect triggerAspect = new Aspect().Include<TriggerComponent>();
		readonly Queue<Tuple<string,Entity,Entity>> triggerQueue = new Queue<Tuple<string,Entity,Entity>>(16);

		
		public Aspect GetAspect()
		{
			return Aspect.Empty;
		}

		
		public void Add( IGameState gs, Entity e )
		{
		}

		
		public void Remove( IGameState gs, Entity e )
		{
		}

		
		public void Trigger( string target, Entity source, Entity activator )
		{
			triggerQueue.Enqueue( Tuple.Create( target, source, activator ) );
		}

		
		public void Update( IGameState gs, GameTime gameTime )
		{
			var triggers = new List<TriggerComponent>(10);

			//	clean trigger flags :
			foreach ( var e in gs.QueryEntities(triggerAspect) )
			{
				var trigger = e.GetComponent<TriggerComponent>();

				trigger.Reset();
				
				triggers.Add( trigger );
			}

			//	set new triggers :
			while ( triggerQueue.Any() )
			{
				var tuple = triggerQueue.Dequeue();

				var target		=	tuple.Item1;
				var source		=	tuple.Item2;
				var activator	=	tuple.Item3;

				if (!string.IsNullOrWhiteSpace(target))
				{
					//	activate all named triggers :
					foreach ( var trigger in triggers )
					{
						if (trigger.Name==target)
						{
							trigger.Set( activator, true );
						}
					}
				}

				//	activate self :
				source?.GetComponent<TriggerComponent>()?.Set(activator, false);
			}
		}
	}
}
