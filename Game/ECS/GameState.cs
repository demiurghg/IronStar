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

namespace IronStar.ECS
{
	public sealed partial class GameState : DisposableBase
	{
		public const int MaxSystems         =   BitSet.MaxBits;
		public const int MaxComponentTypes  =   BitSet.MaxBits;

		public readonly Game Game;

		readonly EntityCollection       entities;
		readonly SystemCollection       systems;
		readonly ComponentCollection    components;

		readonly Bag<Entity>        spawned;
		readonly HashSet<uint>      killed;

		readonly HashSet<Entity>    refreshed;

		readonly GameServiceContainer services;
		public GameServiceContainer Services { get { return services; } }

		readonly Bag<IComponent>    sleeping;

		readonly EntityFactoryCollection factories;


		/// <summary>
		/// Game state constructor
		/// </summary>
		/// <param name="game"></param>
		public GameState( Game game )
		{
			ECSTypeManager.Scan();

			this.Game   =   game;

			entities        =   new EntityCollection();
			systems         =   new SystemCollection(this);
			components      =   new ComponentCollection();

			spawned         =   new Bag<Entity>();
			killed          =   new HashSet<uint>();
			sleeping        =   new Bag<IComponent>();
			refreshed       =   new HashSet<Entity>();

			services        =   new GameServiceContainer();

			factories       =   new EntityFactoryCollection(
									Misc.GetAllClassesWithAttribute<EntityFactoryAttribute>()
										.ToDictionary(
											t0 => t0.GetAttribute<EntityFactoryAttribute>().ClassName,
											t1 => (EntityFactory)Activator.CreateInstance( t1 )
										)
									);
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

				foreach ( var s in sleeping ) s.Removed( this );

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
			//	add all spawned entities :
			foreach ( var e in spawned )
			{
				entities.Add( e.ID, e );
				Refresh( e );
			}
			spawned.Clear();

			//	refresh entities and run systems :
			RefreshEntities();

			//	run sysytems :
			foreach ( var system in systems )
			{
				system.System.Update( this, gameTime );
			}

			//	kill entities marked to kill :
			foreach ( var id in killed ) { KillInternal( id ); }
			killed.Clear();

			//	make static entities sleeping :
			MakeStaticEntitiesSleeping();
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
			return factories[classname].Spawn( this );
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


		void KillInternal( uint id )
		{
			var entity = entities[ id ];
			entities.Remove( id );
			Refresh( entity );

			components.RemoveAllComponents( id, c => c.Removed( this ) );
		}


		void KillAllInternal()
		{
			var killList = entities.Keys.ToArray();

			foreach ( var id in killList )
			{
				KillInternal( id );
			}
		}


		void MakeStaticEntitiesSleeping()
		{
			var ents = QueryEntities<Static>();

			foreach ( var e in ents )
			{
				//	do not call Removed, this will keep statefull objects alive
				components.RemoveAllComponents( e.ID, c => sleeping.Add( c ) );
				entities.Remove( e.ID );
			}
		}

		void Refresh( Entity e )
		{
			refreshed.Add( e );
		}


		void RefreshEntities()
		{
			foreach ( var e in refreshed )
			{
				foreach ( var system in systems )
				{
					system.Changed(e);
				}
			}

			refreshed.Clear();
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Movement stuff :
		-----------------------------------------------------------------------------------------------*/

		public bool Teleport( Entity e, Vector3 position, Quaternion rotation )
		{
			var t = e.GetComponent<Transform>();

			if (t!=null)
			{
				t.Position	=	position;
				t.Rotation	=	rotation;
				
				if (!e.ContainsComponent<Teleport>())
				{
					e.AddComponent(new Teleport());
				}
				
				return true;
			}
			else
			{
				Log.Warning("GameState : cann't teleport entity #{0} -- missing transform", e.ID);
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

			component.Added( this, entity );
			
			Refresh( entity );
		}


		public void RemoveEntityComponent( Entity entity, IComponent component )
		{
			if (entity==null) throw new ArgumentNullException("entity");
			if (component==null) throw new ArgumentNullException("component");

			component.Removed( this );

			entity.ComponentMapping &= ~ECSTypeManager.GetComponentBit( component.GetType() );

			components.RemoveComponent( entity.ID, component );

			Refresh( entity );
		}


		public TComponent GetEntityComponent<TComponent>( Entity entity ) where TComponent: IComponent
		{
			return components.GetComponent<TComponent>( entity.ID );
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
				.ToArray();
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
