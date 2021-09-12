using System;
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

namespace IronStar.ECS
{
	public sealed partial class GameState : DisposableBase, IGameState
	{
		struct FactoryData
		{
			public FactoryData( Entity e, EntityFactory f ) { Entity = e; Factory = f; }
			public readonly Entity Entity;
			public readonly EntityFactory Factory;
		}

		struct TeleportData
		{
			public TeleportData( Entity e, Vector3 t, Quaternion r ) { Entity = e; Translation = t; Rotation = r; }
			public readonly Entity Entity;
			public readonly Vector3 Translation;
			public readonly Quaternion Rotation;
		}

		struct ComponentData
		{
			public ComponentData ( Entity e, IComponent c ) { Entity = e; Component = c; }
			public readonly Entity Entity;
			public readonly IComponent Component;
		}

		public const int MaxSystems			=	BitSet.MaxBits;
		public const int MaxComponentTypes	=	BitSet.MaxBits;

		object lockObj = new object();

		public Game Game { get { return game; } }
		public ContentManager Content { get { return content; } }
		readonly ContentManager content;
		readonly Game game;
		readonly TimeSpan timeStep;

		readonly EntityCollection			entities;
		readonly SystemCollection			systems;
		readonly ComponentCollection		components;

		readonly ConcurrentQueue<Entity>		spawnQueue;
		readonly ConcurrentQueue<FactoryData>	factoryQueue;
		readonly ConcurrentQueue<TeleportData>	teleportQueue;
		readonly ConcurrentQueue<ComponentData>	componentToRemove;
		readonly ConcurrentQueue<Entity>		killQueue;
		readonly HashSet<Entity>				refreshed;
		readonly ConcurrentQueue<Action>		invokeQueue;
		uint									killAllBarrierId = 0;

		readonly GameServiceContainer services;
		public GameServiceContainer Services { get { return services; } }

		readonly EntityFactoryCollection	factories;

		public event	EventHandler Reloading;

		Thread updateThread;
		bool terminate = false;


		/// <summary>
		/// Game state constructor
		/// </summary>
		/// <param name="game"></param>
		public GameState( Game game, ContentManager content, TimeSpan timeStep )
		{
			ECSTypeManager.Scan();

			this.game		=	game;
			this.content	=	content;
			this.timeStep	=	timeStep;

			entities	=	new EntityCollection();
			systems		=	new SystemCollection(this);
			components	=	new ComponentCollection();

			spawnQueue			=	new ConcurrentQueue<Entity>();
			factoryQueue		=	new ConcurrentQueue<FactoryData>();
			teleportQueue		=	new ConcurrentQueue<TeleportData>();
			componentToRemove	=	new ConcurrentQueue<ComponentData>();
			killQueue			=	new ConcurrentQueue<Entity>();
			refreshed			=	new HashSet<Entity>();
			invokeQueue			=	new ConcurrentQueue<Action>();

			services	=	new GameServiceContainer();

			factories	=	new EntityFactoryCollection();

			Game.Reloading += Game_Reloading;
		}


		public void Start()
		{
			updateThread				=	new Thread( UpdateParallelLoop );
			updateThread.Name			=	"ECS Update Thread";
			updateThread.IsBackground	=	true;
			updateThread.Start();
		}

		
		private void Game_Reloading( object sender, EventArgs e )
		{
			Reloading?.Invoke(sender, e);
		}


		bool IsUpdateThread()
		{
			return true;
			return Thread.CurrentThread.ManagedThreadId == updateThread.ManagedThreadId;
		}


		/// <summary>
		/// Disposes stuff
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing )
			{
				terminate = true;
				updateThread.Join();

				Game.Reloading -= Game_Reloading;

				KillAll();

				//	just in case
				RefreshEntities();
				RefreshEntities();

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
			foreach ( var system in systems )
			{
				(system.System as IRenderer)?.Render( this, gameTime );
			}

			RefreshEntities();

			foreach ( var system in systems )
			{
				system.System.Update( this, gameTime );
			}
		}


		public void UpdateParallelLoop()
		{
			long	 frames			=	0;
			TimeSpan dt				=	timeStep;
			TimeSpan currentTime	=	GameTime.CurrentTime;
			TimeSpan accumulator	=	TimeSpan.Zero;

			while (!terminate)
			{
				TimeSpan newTime	=	GameTime.CurrentTime;
				TimeSpan frameTime	=	newTime - currentTime;
				currentTime			=	newTime;

				accumulator	+=	frameTime;

				while (accumulator >= dt)
				{
					//RefreshEntities();

					//foreach ( var system in systems )
					//{
					//	system.System.Update( this, new GameTime(dt, frames) );
					//}

					accumulator -= dt;
					frames++;
				}

				Thread.Sleep(1);
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
		/// <typeparam name="TSystem"></typeparam>
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
				Dictionary<uint,IComponent> componentDict;
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
			FactoryData fd;
			ComponentData cd;
			TeleportData td;

			while (invokeQueue.TryDequeue(out a))
			{
				a.Invoke();
			}

			while (spawnQueue.TryDequeue(out e))
			{
				entities.Add(e);
				Refresh(e);
			}

			while (factoryQueue.TryDequeue(out fd))
			{
				fd.Factory.Construct( fd.Entity, this );
			}

			while (teleportQueue.TryDequeue(out td))
			{
				TeleportInternal( td.Entity, td.Translation, td.Rotation );
			}

			while (componentToRemove.TryDequeue(out cd))
			{
				RemoveEntityComponentInternal( cd.Entity, cd.Component );
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


		public void Invoke( Action action )
		{
			invokeQueue.Enqueue( action );
		}


		void SpawnInternal( Entity e )
		{
			entities.Add( e );
			Refresh( e );
		}


		void SpawnFactoryInternal( Entity e, string classname )
		{
			EntityFactory factory;

			if (factories.TryGetValue( classname, out factory ))
			{
				factory.Construct(e, this);
			}
			else
			{
				Log.Warning("Factory {0} not found. Empty entity is spawned", classname);
			}

			SpawnInternal( e );
		}


		void KillAllInternal()
		{
			if (killAllBarrierId!=0)
			{
				var killList = entities.GetSnapshot();

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
				components.RemoveAllComponents( entity.ID, c => {} );

				Refresh( entity );
			}
		}

		void TeleportInternal( Entity e, Vector3 p, Quaternion r )
		{
			var t = e.GetComponent<Transform>();

			if (t!=null)
			{
				t.Position	=	p;
				t.Rotation	=	r;
			}
			else
			{
				Log.Warning("Teleport: {0} has not {1} component", e, nameof(Transform) );
				//AddEntityComponentInternal( e, new Transform(p,r) );
			}
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Entity stuff :
		-----------------------------------------------------------------------------------------------*/

		public Entity Spawn()
		{
			var e = new Entity( this, IdGenerator.Next() );

			spawnQueue.Enqueue( e );

			return e;
		}


		public Entity Spawn( string classname )
		{
			var e = new Entity( this, IdGenerator.Next() );

			spawnQueue.Enqueue( e );

			EntityFactory factory;

			if (factories.TryGetValue( classname, out factory ))
			{
				if (IsUpdateThread())
				{
					factory.Construct(e,this);
				}
				else
				{
					factoryQueue.Enqueue( new FactoryData(e, factory) );
				}
			}
			else
			{
				Log.Warning("Factory {0} not found. Empty entity is spawned", classname);
			}

			return e;
		}


		public Entity Spawn( string classname, Vector3 position, Quaternion rotation )
		{
			var e = Spawn(classname);

			Teleport( e, position, rotation );
			
			return e;
		}


		void Refresh ( Entity entity )
		{
			if (entity==null) throw new ArgumentNullException("entity");

			refreshed.Add( entity );
		}


		public void Kill( Entity e )
		{
			killQueue.Enqueue( e );
		}


		public void KillAll()
		{
			killAllBarrierId = IdGenerator.Next();
		}


		/*-----------------------------------------------------------------------------------------------
		 *	Movement stuff :
		-----------------------------------------------------------------------------------------------*/

		/// <summary>
		/// Teleports given entity to new place defined by position and rotation.
		/// This method executed immediately from udpate thread.
		/// If called outside of the update thread transformation will be applied at the beginning of the next update frame
		/// </summary>
		/// <param name="e">Entity</param>
		/// <param name="position">New entity position</param>pp
		/// <param name="rotation">New entity rotation</param>
		public void Teleport( Entity e, Vector3 position, Quaternion rotation )
		{
			if (IsUpdateThread())
			{
				TeleportInternal( e, position, rotation );
			}
			else
			{
				teleportQueue.Enqueue( new TeleportData( e, position, rotation ) );
			}
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
			if (!IsUpdateThread()) throw new InvalidOperationException(nameof(AddEntityComponent) + " must be called within update thread");

			AddEntityComponentInternal(entity, component);
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
			if (!IsUpdateThread()) throw new InvalidOperationException(nameof(RemoveEntityComponent) + " must be called within update thread");

			componentToRemove.Enqueue( new ComponentData( entity, component ) );
		}


		private void AddEntityComponentInternal( Entity entity, IComponent component )
		{
			entity.ComponentMapping |= ECSTypeManager.GetComponentBit( component.GetType() );
			components.AddComponent( entity.ID, component );

			Refresh( entity );
		}


		private void RemoveEntityComponentInternal( Entity entity, IComponent component )
		{
			entity.ComponentMapping &= ~ECSTypeManager.GetComponentBit( component.GetType() );
			components.RemoveComponent( entity.ID, component );

			Refresh( entity );
		}


		/// <summary>
		/// Gets entity's component of given type
		/// This method must be called within update thread: in entity factrory or system
		/// </summary>
		/// <typeparam name="TComponent">Component type</typeparam>
		/// <param name="entity">Entity to get component from</param>
		/// <returns>Component</returns>
		public TComponent GetEntityComponent<TComponent>( Entity entity ) where TComponent: IComponent
		{
			if (!IsUpdateThread()) 
			{
				throw new InvalidOperationException(nameof(GetEntityComponent) + " must be called within update thread");
			}
			return components.GetComponent<TComponent>( entity.ID );
		}


		/// <summary>
		/// Gets entity's component of given type
		/// This method must be called within update thread: in entity factrory or system
		/// </summary>
		/// <param name="entity">Entity to get component from</param>
		/// <param name="componentType">Component type</param>
		/// <returns></returns>
		/// <returns>Component</returns>
		public IComponent GetEntityComponent( Entity entity, Type componentType )
		{
			if (!IsUpdateThread()) 
			{
				throw new InvalidOperationException(nameof(GetEntityComponent) + " must be called within update thread");
			}
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
