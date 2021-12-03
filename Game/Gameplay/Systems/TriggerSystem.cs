using System;
using System.Collections.Concurrent;
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
		struct TriggerEvent
		{
			public TriggerEvent( string target, Entity source, Entity activator )
			{
				this.Target	=	target;
				SourceId	=	source==null ? 0 : source.ID;
				ActivatorId	=	activator==null ? 0 : activator.ID;
			}
			public readonly string Target;
			public readonly uint SourceId;
			public readonly uint ActivatorId;
		}

		Aspect triggerAspect = new Aspect().Include<TriggerComponent>();
		readonly ConcurrentQueue<TriggerEvent> triggerQueue = new ConcurrentQueue<TriggerEvent>();

		
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
			triggerQueue.Enqueue( new TriggerEvent(target,source,activator) );
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
			TriggerEvent triggerEvent;

			while ( triggerQueue.TryDequeue(out triggerEvent) )
			{
				var target		=	triggerEvent.Target;
				var source		=	gs.GetEntity( triggerEvent.SourceId );
				var activator	=	gs.GetEntity( triggerEvent.ActivatorId );

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
