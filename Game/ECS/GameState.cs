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

namespace IronStar.ECS
{
	public sealed partial class GameState : DisposableBase, IGameState
	{
		public const int MaxSystems			=	BitSet.MaxBits;
		public const int MaxComponentTypes	=	BitSet.MaxBits;

		object lockObj = new object();

		public Game Game { get { return game; } }
		public ContentManager Content { get { return content; } }
		readonly ContentManager content;
		readonly Game game;

		readonly EntityCollection		entities;
		readonly SystemCollection		systems;
		readonly ComponentCollection	components;

		readonly ConcurrentQueue<Entity>	spawnedQueue;
		readonly ConcurrentQueue<Entity>	killedQueue;
		readonly ConcurrentQueue<Entity>	refreshedQueue;
		readonly ConcurrentQueue<Action>	invokeQueue;
		readonly ConcurrentQueue<Tuple<Entity,IComponent>> componentsToAdd;
		readonly ConcurrentQueue<Tuple<Entity,IComponent>> componentsToRemove;

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
		public GameState( Game game, ContentManager content )
		{
			ECSTypeManager.Scan();

			this.game		=	game;
			this.content	=	content;

			entities	=	new EntityCollection();
			systems		=	new SystemCollection(this);
			components	=	new ComponentCollection();

			spawnedQueue	=	new ConcurrentQueue<Entity>();
			killedQueue		=	new ConcurrentQueue<Entity>();
			refreshedQueue	=	new ConcurrentQueue<Entity>();
			invokeQueue		=	new ConcurrentQueue<Action>();

			componentsToAdd		=	new ConcurrentQueue<Tuple<Entity,IComponent>>();
			componentsToRemove	=	new ConcurrentQueue<Tuple<Entity,IComponent>>();

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


		/// <summary>
		/// Disposes stuff
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing )
			{
				Game.Reloading -= Game_Reloading;

				KillAllInternal();

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


		/// <summary>
		/// Updates game state
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update( GameTime gameTime )
		{
			RefreshEntities();

			foreach ( var system in systems )
			{
				system.System.Update( this, gameTime );
			}

			PrintState();
		}


		public void UpdateParallelLoop()
		{
			while (!terminate)
			{
				RefreshEntities();

				foreach ( var system in systems )
				{
					//system.System.Update( this, gameTime );
				}
			}


		}


		void RefreshEntities()
		{
			Entity e;
			Action a;

			while (invokeQueue.TryDequeue(out a))
			{
				a.Invoke();
			}

			//	spawn entities :
			while (spawnedQueue.TryDequeue(out e))
			{
				entities.Add( e );
				Refresh( e );
			}

			AddEntityComponentsInternal();

			RemoveEntityComponentsInternal();

			//	kill entities marked to kill :
			while (killedQueue.TryDequeue(out e))
			{
				KillInternal(e);
			}

			//	refresh component and system bindings :
			while (refreshedQueue.TryDequeue(out e))
			{
				foreach ( var system in systems )
				{
					system.Changed(e);
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
		 *	Actions :
		-----------------------------------------------------------------------------------------------*/

		public void Invoke ( Action action )
		{
			if (action!=null)
			{
				invokeQueue.Enqueue( action );
			}
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Entity stuff :
		-----------------------------------------------------------------------------------------------*/

		public Entity Spawn()
		{
			var entity = new Entity( this, IdGenerator.Next() );

			spawnedQueue.Enqueue( entity );

			return entity;
		}


		public Entity Spawn( string classname )
		{
			EntityFactory factory;

			if (factories.TryGetValue( classname, out factory ))
			{
				return factory.Spawn(this);
			}
			else
			{
				Log.Warning("Factory {0} not found. Empty entity is spawned", classname);
				return Spawn();;
			}
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

			refreshedQueue.Enqueue( entity );
		}


		public Entity GetEntity( uint id )
		{
			return entities[ id ];
		}


		public void Kill( Entity e )
		{
			if (e!=null) killedQueue.Enqueue( e );
		}


		void KillInternal( Entity entity )
		{
			if (entity!=null)
			{
				if ( entities.Remove( entity ) )
				{
					RemoveAllEntityComponent( entity );
					Refresh( entity );
				}
				else
				{
					if (spawnedQueue.Contains(entity))
					{
						Log.Warning("Spawn queue contains killed entity!");
					}
				}
			}
		}


		void KillAllInternal()
		{
			var killList = entities.GetSnapshot();

			foreach ( var e in killList )
			{
				KillInternal( e );
			}
		}


		public void KillAll()
		{
			KillAllInternal();
			RefreshEntities();
		}


		public bool Exists( uint id )
		{
			return entities.Contains(id);
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Movement stuff :
		-----------------------------------------------------------------------------------------------*/

		public bool Teleport( Entity e, Vector3 position, Quaternion rotation )
		{
			// #TODO -- force refresh entity to destroy and create again

			var transform = e.GetComponent<Transform>();

			if (transform!=null)
			{
				transform.Position	=	position;
				transform.Rotation	=	rotation;
				return true;
			}
			else
			{
				Log.Warning("Spawn(classname,position,rotation) : missing transform component");
				return false;
			}
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Component stuff :
		-----------------------------------------------------------------------------------------------*/

		const bool deferredComponents = false;

		public void AddEntityComponent( Entity entity, IComponent component )
		{
			if (entity==null) throw new ArgumentNullException("entity");
			if (component==null) throw new ArgumentNullException("component");

			//	deferred addition :
			if (deferredComponents)
			{
				componentsToAdd.Enqueue( new Tuple<Entity, IComponent>( entity, component ) );
			}
			else
			{
				components.AddComponent( entity.ID, component );
				entity.ComponentMapping |= ECSTypeManager.GetComponentBit( component.GetType() );

				Refresh( entity );
			}
		}


		public void RemoveEntityComponent( Entity entity, IComponent component )
		{
			if (entity==null) throw new ArgumentNullException("entity");
			if (component==null) throw new ArgumentNullException("component");

			componentsToRemove.Enqueue( new Tuple<Entity, IComponent>( entity, component ) );
		}


		private void AddEntityComponentsInternal()
		{
			if (!deferredComponents) return;

			Tuple<Entity,IComponent> entry;

			while (componentsToAdd.TryDequeue( out entry ))
			{
				var entity		=	entry.Item1;
				var component	=	entry.Item2;

				entity.ComponentMapping |= ECSTypeManager.GetComponentBit( component.GetType() );
				components.AddComponent( entity.ID, component );

				Refresh( entity );
			}
		}


		private void RemoveEntityComponentsInternal()
		{
			Tuple<Entity,IComponent> entry;

			while (componentsToRemove.TryDequeue( out entry ))
			{
				var entity		=	entry.Item1;
				var component	=	entry.Item2;

				entity.ComponentMapping &= ~ECSTypeManager.GetComponentBit( component.GetType() );
				components.RemoveComponent( entity.ID, component );

				Refresh( entity );
			}
		}


		void RemoveAllEntityComponent( Entity entity )
		{
			entity.ComponentMapping = 0;
			components.RemoveAllComponents( entity.ID, c => {} );

			Refresh( entity );
		}

		
		public TComponent GetEntityComponent<TComponent>( Entity entity ) where TComponent: IComponent
		{
			return components.GetComponent<TComponent>( entity.ID );
		}

		
		public IComponent GetEntityComponent( Entity entity, Type componentType )
		{
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

		public IEnumerable<Entity> QueryEntities( Aspect aspect )
		{
			return entities.Query( aspect );
		}
	}
}
