﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;
using System.Reflection;
using Fusion;
using System.Runtime.Remoting;
using Fusion.Core.Content;
using Fusion.Engine.Tools;
using System.Collections.Concurrent;
using System.Collections;
using System.Threading;
using Fusion.Core.Shell;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BEPUutilities.Threading;

namespace IronStar.ECS
{
	public sealed partial class GameState : DisposableBase, IGameState
	{
		struct SpawnData
		{
			public SpawnData( Entity e, IFactory f ) { Entity = e; Factory = f; }
			public readonly Entity Entity;
			public readonly IFactory Factory;
		}

		struct ComponentData
		{
			public ComponentData ( Entity e, IComponent c ) { Entity = e; Component = c; }
			public readonly Entity Entity;
			public readonly IComponent Component;
		}

		public bool Paused { get; set; } = false;

		public const int MaxSystems			=	BitSet.MaxBits;
		public const int MaxComponentTypes	=	BitSet.MaxBits;

		object lockObj = new object();

		public Game Game { get { return game; } }
		public ContentManager Content { get { return content; } }
		readonly ContentManager content;
		readonly Game game;

		readonly EntityCollection			entities;
		readonly SystemCollection			systems;
		readonly ComponentCollection		components;

		readonly ConcurrentQueue<SpawnData>		spawnQueue3;
		readonly ConcurrentQueue<Entity>		killQueue;
		readonly ConcurrentQueue<ComponentData>	componentToRemove;
		readonly ConcurrentQueue<ComponentData>	componentToAdd;
		readonly HashSet<Entity>				refreshed;
		uint									killAllBarrierId = 0;

		readonly GameServiceContainer services;
		public GameServiceContainer Services { get { return services; } }

		readonly EntityFactoryCollection	factories;

		public event	EventHandler Reloading;

		readonly Stopwatch stopwatch = new Stopwatch();
		readonly Thread mainThread;
		bool terminate = false;

		/// <summary>
		/// Game state constructor
		/// </summary>
		/// <param name="game"></param>
		public GameState( Game game, ContentManager content )
		{
			mainThread		=	Thread.CurrentThread;

			this.game		=	game;
			this.content	=	content;

			entities	=	new EntityCollection();
			systems		=	new SystemCollection(this);
			components	=	new ComponentCollection();

			spawnQueue3			=	new ConcurrentQueue<SpawnData>();
			componentToRemove	=	new ConcurrentQueue<ComponentData>();
			componentToAdd		=	new ConcurrentQueue<ComponentData>();
			killQueue			=	new ConcurrentQueue<Entity>();
			refreshed			=	new HashSet<Entity>();
			
			services	=	new GameServiceContainer();

			factories	=	new EntityFactoryCollection();

			Game.Reloading += Game_Reloading;
		}

		
		private void Game_Reloading( object sender, EventArgs e )
		{
			Reloading?.Invoke(sender, e);
		}


		bool IsUpdateThread()
		{
			return Thread.CurrentThread.ManagedThreadId == mainThread.ManagedThreadId;
		}


		void CheckUpdateThread(string methodName)
		{
			if (!IsUpdateThread()) throw new InvalidOperationException(methodName + " must be called from UPDATE thread");
		}


		/// <summary>
		/// Disposes stuff
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing )
			{
				KillAll();
				RefreshEntities();

				Game.Reloading -= Game_Reloading;

				foreach ( var systemWrapper in systems )
				{
					var system = systemWrapper.System;
					Game.Components.Remove(	system as IGameComponent );
					( system as IDisposable )?.Dispose();
				}
			}

			base.Dispose( disposing );
		}

		/*-----------------------------------------------------------------------------------------
		 *	Updates :
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// Updates game state
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update( GameTime gameTime )
		{
			using ( new CVEvent( "ECS Update" ) )
			{
				var maxTime = TimeSpan.FromMilliseconds(20);

				stopwatch.Reset();
				stopwatch.Start();

				RefreshEntities();

				foreach ( var system in systems )
				{
					system.Update( this, gameTime );
				}

				stopwatch.Stop();
				if (stopwatch.Elapsed > maxTime)
				{
					Log.Warning("LOOP TIME {0} > DT {1}", stopwatch.Elapsed, maxTime);

					/*foreach ( var system in systems )
					{
						if (system.ProfilingTime.Ticks > maxTime.Ticks / 10)
						{
							Log.Warning("   {0} : {1}", system.ProfilingTime, system.System.GetType().Name );
						}
					}*/
				}
			}
		}

		
		/// <summary>
		/// Gets gamestate's service
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		/// <returns></returns>
		public TService GetService<TService>() where TService : class
		{
			lock (lockObj)
			{
				return Services.GetService<TService>();
			}
		}


		/// <summary>
		/// Gets all system inherited from TSystem
		/// </summary>
		/// <typeparam name=
		/// "TSystem"></typeparam>
		/// <returns></returns>
		public IEnumerable<TSystem> GatherSystems<TSystem>()
		{
			lock (lockObj)
			{
				var type = typeof(TSystem);
				return systems
					.Where( sys1 => type.IsAssignableFrom( sys1.System.GetType() ) )
					.Select( sys2 => (TSystem)sys2.System )
					.ToArray();
			}
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Debug stuff :
		-----------------------------------------------------------------------------------------------*/

		public void PrintState()
		{
			return;

			var con = Game.GetService<GameConsole>();

			con.DrawDebugText(Color.White, "-------- ECS Game State --------");

			con.DrawDebugText(Color.White, "   entities : {0}", entities.Count );

			foreach ( var componentType in ECSTypeManager.GetComponentTypes() )
			{
				ComponentBuffer componentDict;
				if (components.TryGetValue( componentType, out componentDict ))
				{
					con.DrawDebugText(Color.White, "  component : {0} : {1}", componentType.Name.Replace("Component", ""), componentDict.Count );
				}
			}

			con.DrawDebugText(Color.White, "--------------------------------");
		}


		/*-----------------------------------------------------------------------------------------------
		 *	Structured change tracking :
		-----------------------------------------------------------------------------------------------*/

		void RefreshEntities()
		{
			Entity e;
			Action a;
			ComponentData cd;
			SpawnData sd;

			while (spawnQueue3.TryDequeue(out sd))
			{
				entities.Add( sd.Entity );
				sd.Factory.Construct( sd.Entity, this );
				Refresh( sd.Entity );
			}

			while (componentToRemove.TryDequeue(out cd))
			{
				RemoveEntityComponentImmediate( cd.Entity, cd.Component );
			}

			while (killQueue.TryDequeue(out e))
			{
				KillInternal(e);
			}

			KillAllInternal();

			//	refresh component and system bindings :
			foreach (var re in refreshed)
			{
				foreach ( var system in systems )
				{
					system.Changed(re);
				}
			}
			
			refreshed.Clear();
		}


		void KillAllInternal()
		{
			if (killAllBarrierId!=0)
			{
				var killList = entities.Select( pair => pair.Value ).ToArray();

				foreach ( var e in killList )
				{
					if (e.ID<=killAllBarrierId)
					{
						KillInternal( e );
					}
				}

				killAllBarrierId = 0;
			}
		}


		void KillInternal( Entity entity )
		{
			if ( entities.Remove( entity ) )
			{
				entity.ComponentMapping = 0;
				components.RemoveAllComponents( entity.ID );

				Refresh( entity );
			}
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Entity stuff :
		-----------------------------------------------------------------------------------------------*/

		public Entity Spawn(IFactory factory)
		{
			var entity = new Entity( this, IdGenerator.Next() );

			spawnQueue3.Enqueue( new SpawnData(entity, factory) );

			return entity;
		}

		void Refresh ( Entity entity )
		{
			if (entity==null) throw new ArgumentNullException("entity");

			refreshed.Add( entity );
		}


		/// <summary>
		/// Adds entity to kill queue.
		/// </summary>
		/// <param name="e"></param>
		public void Kill( Entity e )
		{
			killQueue.Enqueue( e );
		}


		/// <summary>
		/// Kills all entities created before this call.
		/// </summary>
		public void KillAll()
		{
			killAllBarrierId = IdGenerator.Next();
		}


		/*-----------------------------------------------------------------------------------------------
		 *	Component stuff :
		-----------------------------------------------------------------------------------------------*/

		/// <summary>
		/// Immediately adds component to the given entity.
		/// This method must be called within update thread: in entity factrory or system
		/// </summary>
		/// <param name="entity">Entity to add component to</param>
		/// <param name="component">Component to add</param>
		public void AddEntityComponent( Entity entity, IComponent component )
		{
			if (entity==null) throw new ArgumentNullException("entity");
			if (component==null) throw new ArgumentNullException("component");

			//CheckUpdateThread(nameof(AddEntityComponent));
			AddEntityComponentImmediate(entity, component);
		}

		/// <summary>
		/// Add component to removal queue. Component will be removed at the beginning of the next update frame.
		/// This method must be called within update thread: in entity factrory or system
		/// </summary>
		/// <param name="entity">Entity to remove component from</param>
		/// <param name="component">Component to remove</param>
		public void RemoveEntityComponent( Entity entity, IComponent component )
		{
			if (entity==null) throw new ArgumentNullException("entity");
			if (component==null) throw new ArgumentNullException("component");
			//CheckUpdateThread(nameof(RemoveEntityComponent));

			componentToRemove.Enqueue( new ComponentData( entity, component ) );
		}


		private void AddEntityComponentImmediate( Entity entity, IComponent component )
		{
			entity.ComponentMapping |= ECSTypeManager.GetComponentBit( component.GetType() );
			components.AddComponent( entity.ID, component );

			Refresh( entity );
		}


		private void RemoveEntityComponentImmediate( Entity entity, IComponent component )
		{
			entity.ComponentMapping &= ~ECSTypeManager.GetComponentBit( component.GetType() );
			components.RemoveComponent( entity.ID, component );

			Refresh( entity );
		}


		/// <summary>
		/// Gets entity's component of given type
		/// Result depends on thread where if called from.
		/// In update thread this method returns latest component value.
		/// In main thread this method returns interpolated or buffered value.
		/// </summary>
		/// <param name="entity">Entity to get component from</param>
		/// <param name="componentType">Component type</param>
		/// <returns></returns>
		/// <returns>Component</returns>
		public IComponent GetEntityComponent( Entity entity, Type componentType )
		{
			//CheckUpdateThread(nameof(GetEntityComponent));

			return components.GetComponent( entity.ID, componentType );
		}

		/*-----------------------------------------------------------------------------------------------
		 *	System stuff :
		-----------------------------------------------------------------------------------------------*/

		public void AddSystem ( ISystem system )
		{
			lock (lockObj)
			{
				if (system==null) throw new ArgumentNullException("system");

				services.AddService( system.GetType(), system );
				systems.Add( system );

				if (system is IGameComponent)
				{
					Game.Components.Add( (IGameComponent)system );
				}
			}
		}


		/*-----------------------------------------------------------------------------------------------
		 *	Queries :
		-----------------------------------------------------------------------------------------------*/

		public bool Exists( uint id )
		{
			return entities.Contains(id);
		}

		public Entity GetEntity( uint id )
		{
			return entities[ id ];
		}


		public IEnumerable<Entity> QueryEntities( Aspect aspect )
		{
			return entities.Query( aspect );
		}
	}
}
