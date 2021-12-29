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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BEPUutilities.Threading;
using System.IO;

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
			public ComponentData ( Entity e, Type type ) { Entity = e; ComponentType = type; }
			public readonly Entity Entity;
			public readonly Type ComponentType;
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
		readonly bool debug;
		bool terminate = false;
		public readonly Domain Domain;

		/// <summary>
		/// Game state constructor
		/// </summary>
		/// <param name="game"></param>
		public GameState( Domain domain, Game game, ContentManager content, bool debug )
		{
			this.Domain		=	domain;
			this.debug		=	debug;
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

		public IEnumerable<ISystem> Systems
		{
			get { return systems.Select( s => s.System ); }
		}

		/// <summary>
		/// Updates game state
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update( GameTime gameTime )
		{
			using ( new CVEvent( "ECS Update" ) )
			{
				RefreshEntities();

				foreach ( var system in systems )
				{
					system.Update( this, gameTime );

					CheckTransforms();

					if (forceRefresh)
					{
						RefreshEntities();
						forceRefresh = false;
					}
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

		bool IsLocalID( uint id )
		{
			return (id>>28)==(uint)Domain;
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Debug stuff :
		-----------------------------------------------------------------------------------------------*/

		public void CheckTransforms()
		{
			foreach ( var t in components[typeof(Transform)] )
			{
				var e = entities[t.Key];
				((Transform)(t.Value.Current))?.CheckTransform( "current", e );
				((Transform)(t.Value.Lerped))?.CheckTransform( "lerped", e );
			}
		}

		public void PrintState()
		{
			return;

			var con = Game.GetService<GameConsole>();

			con.DrawDebugText(Color.White, "-------- ECS Game State --------");

			con.DrawDebugText(Color.White, "   entities : {0}", entities.Count );

			foreach ( var componentType in ECSTypeManager.GetComponentTypes() )
			{
				ComponentDictionary componentDict;
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
				RemoveEntityComponentImmediate( cd.Entity, cd.ComponentType );
			}

			while (killQueue.TryDequeue(out e))
			{
				KillInternal(e);
			}

			KillAllInternal();

			//	refresh component and system bindings :
			var refreshList = refreshed.ToArray();
			refreshed.Clear();

			foreach (var re in refreshList)
			{
				foreach ( var system in systems )
				{
					system.Changed(re);
				}
			}
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
			var entity = new Entity( this, IdGenerator.Next(Domain) );

			spawnQueue3.Enqueue( new SpawnData(entity, factory) );

			return entity;
		}


		bool forceRefresh = false;

		public void ForceRefresh()
		{
			forceRefresh = true;
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
			killAllBarrierId = IdGenerator.Next(Domain);
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
		public void RemoveEntityComponent( Entity entity, Type componentType )
		{
			if (entity==null) throw new ArgumentNullException("entity");
			//CheckUpdateThread(nameof(RemoveEntityComponent));

			componentToRemove.Enqueue( new ComponentData( entity, componentType ) );
		}


		private void AddEntityComponentImmediate( Entity entity, IComponent component )
		{
			entity.ComponentMapping |= ECSTypeManager.GetComponentBit( component.GetType() );
			components.AddComponent( entity.ID, component );

			Refresh( entity );
		}


		private void RemoveEntityComponentImmediate( Entity entity, Type componentType )
		{
			entity.ComponentMapping &= ~ECSTypeManager.GetComponentBit( componentType );
			components.RemoveComponent( entity.ID, componentType );

			Refresh( entity );
		}


		/// <summary>
		/// Gets entity's component of given type
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

				if (services.GetService(system.GetType())==null)
				{
					services.AddService( system.GetType(), system );
				}

				systems.Add( system );

				if (system is IGameComponent)
				{
					Game.Components.Add( (IGameComponent)system );
				}
			}
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Save/Load :
		-----------------------------------------------------------------------------------------------*/

		public void Save( Stream stream, TimeSpan time, TimeSpan dt )
		{
			using ( new CVEvent( "GameState.Save" ) )
			{
				using ( var writer = new BinaryWriter( stream ) )
				{
					//	write timing
					writer.Write( time );
					writer.Write( dt );

					//	write entity IDs :
					writer.Write( entities.Count );
					foreach ( var pair in entities )
					{
						writer.Write( pair.Key );
					}

					//	write component data :
					foreach ( var type in ECSTypeManager.GetComponentTypes() )
					{
						var compBuffer  =   components[type];

						writer.Write( compBuffer.Count );

						foreach ( var compPair in compBuffer )
						{
							writer.Write( compPair.Key );
							compPair.Value.Current.Save( this, writer );
						}
					}
				}
			}
		}

		TimeSpan snapshotTimestamp;
		TimeSpan snapshotTimestep;


		public void Load( Stream stream )
		{
			using ( var reader = new BinaryReader( stream ) )
			{
				//	read timing :
				var timestamp	=	reader.Read<TimeSpan>();
				var timestep	=	reader.Read<TimeSpan>();

				//	compare old timestamp and do not load state twice
				if (snapshotTimestamp==timestamp)
				{
					return;
				}

				snapshotTimestamp	=	timestamp;
				snapshotTimestep	=	timestep;

				//	read entity IDs :
				int entityCount = reader.ReadInt32();
				HashSet<uint> newIDs = new HashSet<uint>(entityCount);

				for (int i=0; i<entityCount; i++)
				{
					newIDs.Add( reader.ReadUInt32() );
				}

				//	remove old entities from other domains,
				//	keep local entities alive:
				foreach ( var e in entities )
				{
					if (!e.Value.IsLocalDomain && !newIDs.Contains(e.Value.ID))
					{
						e.Value.Kill();
					}
				}

				//	add new entities :
				foreach ( var id in newIDs )
				{
					if (!entities.Contains(id))
					{
						var e = new Entity(this, id);
						entities.Add( e );
						Refresh( e );
					}
				}

				//	read component data :
				foreach ( var type in ECSTypeManager.GetComponentTypes() )
				{
					var componentDict	=	components[type];
					int componentCount	=	reader.ReadInt32();
					var idsSet			=	new HashSet<uint>(componentCount);

					for (int i=0; i<componentCount; i++)
					{
						uint id = reader.ReadUInt32();
						idsSet.Add(id);
						ComponentTuple tuple;

						if ( componentDict.TryGetValue( id, out tuple ) )
						{
							// #TODO #ECS -- interpolate only interpolatable components
							tuple.Previous = tuple.Current.Clone();
							tuple.Current.Load( this, reader );
						}
						else
						{
							var newComponent = (IComponent)Activator.CreateInstance(type);
							newComponent.Load( this, reader );
							entities[id]?.AddComponent( newComponent );
						}
					}

					var toRemove = componentDict.Keys
									.Except( idsSet )
									.ToArray();

					foreach ( var id in toRemove )
					{
						if (!IsLocalID(id))
						{
							//	#TODO #ECS -- check component removal
							RemoveEntityComponent( entities[id], type );
						}
					}
				}
			}
		}


		public void InterpolateState ( TimeSpan time )
		{
			double	ftimestamp	=	snapshotTimestamp.TotalSeconds;
			double	ftimestep	=	snapshotTimestep.TotalSeconds;
			double	ftime		=	time.TotalSeconds;

			float	factor		=	MathUtil.Clamp( (float)((ftime - ftimestamp)/ftimestep), -0.5f, 2.0f );
			float	dt			=	(float)ftimestep;

			if (factor<-0.1f || factor>1.5f)
			{
				//Log.Warning("FACTOR OUT OF RANGE : {0}", factor);
			}

			components.Interpolate( dt, factor );
		}


		/*-----------------------------------------------------------------------------------------------
		 *	Queries :
		-----------------------------------------------------------------------------------------------*/

		public bool Exists( Entity entity )
		{
			return entity==null ? false : entities.Contains(entity.ID);
		}

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
