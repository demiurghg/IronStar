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

namespace IronStar.ECS
{
	public sealed partial class GameState : DisposableBase
	{
		public const int MaxSystems         =   BitSet.MaxBits;
		public const int MaxComponentTypes  =   BitSet.MaxBits;

		public readonly Game Game;
		public readonly ContentManager Content;

		readonly EntityCollection       entities;
		readonly SystemCollection       systems;
		readonly ComponentCollection    components;

		readonly Bag<Entity>        spawned;
		readonly HashSet<uint>      killed;

		readonly HashSet<Entity>    refreshed;

		readonly GameServiceContainer services;
		public GameServiceContainer Services { get { return services; } }

		readonly EntityFactoryCollection	factories;
		readonly EntityActionCollection		actions;
		readonly Queue<EntityAction>		actionQueue;


		/// <summary>
		/// Game state constructor
		/// </summary>
		/// <param name="game"></param>
		public GameState( Game game, ContentManager content )
		{
			ECSTypeManager.Scan();

			this.Game		=	game;
			this.Content	=	content;

			entities	=	new EntityCollection();
			systems		=	new SystemCollection(this);
			components	=	new ComponentCollection();

			spawned		=	new Bag<Entity>();
			killed		=	new HashSet<uint>();
			refreshed	=	new HashSet<Entity>();

			services	=	new GameServiceContainer();

			factories	=	new EntityFactoryCollection();
			actions		=	new EntityActionCollection();
			actionQueue	=	new Queue<EntityAction>();
		}


		/// <summary>
		/// Disposes stuff
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing )
			{
				KillAllInternal();

				RefreshEntities();

				foreach ( var system in systems )
				{
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
			//	refresh entities and run systems :
			RefreshEntities();

			//	run sysytems :
			foreach ( var system in systems )
			{
				system.System.Update( this, gameTime );
				RefreshEntities();
			}

			PrintState();
		}


		void RefreshEntities()
		{
			//	spawn entities :
			foreach ( var e in spawned ) 
			{
				entities.Add( e.ID, e );
				Refresh( e );
			}
			spawned.Clear();

			//	refresh component and system bindings :
			foreach ( var e in refreshed )
			{
				foreach ( var system in systems )
				{
					system.Changed(e);
				}
			}
			refreshed.Clear();

			//	kill entities marked to kill :
			foreach ( var id in killed ) { KillInternal( id ); }
			killed.Clear();
		}

		
		/// <summary>
		/// Gets gamestate's service
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		/// <returns></returns>
		public TService GetService<TService>() where TService : class
		{
			return Services.GetService<TService>();
		}


		/// <summary>
		/// Gets all system inherited from TSystem
		/// </summary>
		/// <typeparam name="TSystem"></typeparam>
		/// <returns></returns>
		public IEnumerable<TSystem> GatherSystems<TSystem>()
		{
			var type = typeof(TSystem);
			return systems
				.Where( sys1 => type.IsAssignableFrom( sys1.System.GetType() ) )
				.Select( sys2 => (TSystem)sys2.System )
				.ToArray();
		}



		/*-----------------------------------------------------------------------------------------------
		 *	Debug stuff :
		-----------------------------------------------------------------------------------------------*/

		public void PrintState()
		{
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

		public bool Execute( string actionName, Entity target )
		{
			if (string.IsNullOrWhiteSpace(actionName)) 
			{
				return false;
			}

			EntityAction action;

			if ( actions.TryGetValue( actionName, out action ) )
			{
				action.Execute( this, target );
				return true;
			}
			{
				Log.Warning("GameState:Execute -- no such action '{0}'", actionName);
				return false;
			}
		}


		void ExecuteActions()
		{
			// #TODO #ECS -- deferred action execution???
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Entity stuff :
		-----------------------------------------------------------------------------------------------*/

		public Entity Spawn()
		{
			var entity = new Entity( this, IdGenerator.Next() );

			spawned.Add( entity );

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


		public Entity GetEntity( uint id )
		{
			return entities[id];
		}


		public void Kill( uint id )
		{
			killed.Add( id );
		}


		public void Kill( Entity e )
		{
			if (e!=null) killed.Add( e.ID );
		}


		void KillInternal( uint id )
		{
			Entity entity;
			if (entities.TryGetValue(id, out entity))
			{
				entities.Remove( id );
				RemoveAllEntityComponent( entity );
				Refresh( entity );
			}
		}


		void KillAllInternal()
		{
			var killList = entities.Keys.ToArray();

			foreach ( var id in killList )
			{
				KillInternal( id );
			}
		}


		public void KillAll()
		{
			KillAllInternal();
			RefreshEntities();
		}


		void Refresh( Entity e )
		{
			refreshed.Add( e );
		}


		public bool Exists( uint id )
		{
			return entities.ContainsKey(id);
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

		public void AddEntityComponent ( Entity entity, IComponent component )
		{
			if (entity==null) throw new ArgumentNullException("entity");
			if (component==null) throw new ArgumentNullException("component");

			components.AddComponent( entity.ID, component );

			entity.ComponentMapping |= ECSTypeManager.GetComponentBit( component.GetType() );

			Refresh( entity );
		}


		public void RemoveEntityComponent( Entity entity, IComponent component )
		{
			if (entity==null) throw new ArgumentNullException("entity");
			if (component==null) throw new ArgumentNullException("component");

			entity.ComponentMapping &= ~ECSTypeManager.GetComponentBit( component.GetType() );

			components.RemoveComponent( entity.ID, component );

			Refresh( entity );
		}


		public void RemoveAllEntityComponent( Entity entity )
		{
			if (entity==null) throw new ArgumentNullException("entity");

			entity.ComponentMapping = 0;
			components.RemoveAllComponents( entity.ID, c => {} );
			
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
			if (system==null) throw new ArgumentNullException("system");

			services.AddService( system.GetType(), system );
			systems.Add( system );
		}


		/*-----------------------------------------------------------------------------------------------
		 *	System stuff :
		-----------------------------------------------------------------------------------------------*/

		readonly EntityComponentComparer entityComponentComparer = new EntityComponentComparer();

		class EntityComponentComparer : IEqualityComparer<KeyValuePair<uint, IComponent>>
		{
			public bool Equals( KeyValuePair<uint, IComponent> x, KeyValuePair<uint, IComponent> y ) {  return x.Key == y.Key; }
			public int GetHashCode( KeyValuePair<uint, IComponent> obj ) { return obj.Key.GetHashCode(); }
		}


		public IEnumerable<Entity> QueryEntities( Aspect aspect )
		{
			return entities
				.Where( e1 => aspect.Accept(e1.Value) )
				.Select( e2 => e2.Value )
				//.ToArray()
				;
		}


		public IEnumerable<TComponent> QueryComponents<TComponent>() where TComponent: IComponent
		{
			return components[typeof(TComponent)].Select( kv => (TComponent)kv.Value ).ToArray();
		}


		public IEnumerable<Entity> QueryEntities<TComponent>() 
		where TComponent: IComponent
		{
			var	s1	=	components[typeof(TComponent)];
			
			return s1
				.Select( keyValue => entities[ keyValue.Key ] )
				.Where( e => e!=null )
				.ToArray(); 
		}


		public IEnumerable<Entity> QueryEntities<TComponent1, TComponent2>() 
		where TComponent1: IComponent 
		where TComponent2: IComponent
		{
			var	s1	=	components[typeof(TComponent1)];
			var	s2	=	components[typeof(TComponent2)];

			return s1.Intersect( s2, entityComponentComparer )
				.Select( keyValue => entities[ keyValue.Key ] )
				.Where( e => e!=null )
				.ToArray(); 
		}


		public IEnumerable<Entity> QueryEntities<TComponent1, TComponent2, TComponent3>() 
		where TComponent1: IComponent 
		where TComponent2: IComponent
		where TComponent3: IComponent
		{
			var	s1	=	components[typeof(TComponent1)];
			var	s2	=	components[typeof(TComponent2)];
			var	s3	=	components[typeof(TComponent3)];

			return s1
				.Intersect( s2, entityComponentComparer )
				.Intersect( s3, entityComponentComparer )
				.Select( keyValue => entities[ keyValue.Key ] )
				.Where( e => e!=null )
				.ToArray(); 
		}


		public IEnumerable<Entity> QueryEntities<TComponent1, TComponent2, TComponent3, TComponent4>() 
		where TComponent1: IComponent 
		where TComponent2: IComponent
		where TComponent3: IComponent
		where TComponent4: IComponent
		{
			var	s1	=	components[typeof(TComponent1)];
			var	s2	=	components[typeof(TComponent2)];
			var	s3	=	components[typeof(TComponent3)];
			var	s4	=	components[typeof(TComponent4)];

			return s1
				.Intersect( s2, entityComponentComparer )
				.Intersect( s3, entityComponentComparer )
				.Intersect( s4, entityComponentComparer )
				.Select( keyValue => entities[ keyValue.Key ] )
				.Where( e => e!=null )
				.ToArray(); 
		}

	}
}
