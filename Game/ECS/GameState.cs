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

namespace IronStar.ECS
{
	public class GameState : DisposableBase
	{
		public readonly Game Game;

		readonly EntityCollection		entities;
		readonly SystemCollection		systems;
		readonly ComponentCollection	components;

		readonly Bag<Entity>	spawned;
		readonly HashSet<uint>	killed;

		readonly GameServiceContainer services;
		public GameServiceContainer Services { get { return services; } }

		readonly Bag<IComponent>	sleeping;


		/// <summary>
		/// Game state constructor
		/// </summary>
		/// <param name="game"></param>
		public GameState( Game game )
		{
			this.Game	=	game;

			entities		=	new EntityCollection();
			systems			=	new SystemCollection();
			components		=	new ComponentCollection();

			spawned			=	new Bag<Entity>();
			killed			=	new HashSet<uint>();
			sleeping		=	new Bag<IComponent>();

			services		=	new GameServiceContainer();
		}


		/// <summary>
		/// Disposes stuff
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				KillAllInternal();

				foreach ( var s in sleeping ) s.Removed(this);

				foreach ( var system in systems )
				{
					(system as IDisposable)?.Dispose();
				}
			}

			base.Dispose( disposing );
		}


		/// <summary>
		/// Updates game state
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update ( GameTime gameTime )
		{
			//	add all spawned entities :
			foreach ( var e in spawned )
			{
				entities.Add( e.ID, e );
			}

			//	run sysytems :
			foreach ( var system in systems )
			{
				system.Update( this, gameTime );
			}

			//	kill entities marked to kill :
			foreach ( var id in killed )
			{
				KillInternal(id);
			}

			//	clear teleport component :
			components.ClearComponentsOfType<Teleport>();
			MakeStaticEntitiesSleeping();
		}


		/// <summary>
		/// Gets gamestate's service
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		/// <returns></returns>
		public TService GetService<TService>() where TService: class
		{
			return Services.GetService<TService>();
		}


		/*-----------------------------------------------------------------------------------------------
		 *	Entity stuff :
		-----------------------------------------------------------------------------------------------*/

		public Entity Spawn ()
		{
			var entity = new Entity( this, IdGenerator.Next() );
			
			spawned.Add( entity );

			return entity;
		}


		public Entity GetEntity( uint id )
		{
			return entities[id];
		}


		public void Kill ( uint id )
		{
			killed.Add( id );
		}


		void KillInternal( uint id )
		{
			var entity = entities[ id ];
			entities.Remove( id );

			components.RemoveAllComponents( id, c => c.Removed(this) );
		}


		void KillAllInternal()
		{
			var killList = entities.Keys.ToArray();

			foreach ( var id in killList )
			{
				KillInternal(id);
			}
		}


		void MakeStaticEntitiesSleeping()
		{
			var ents = QueryEntities<Static>();

			foreach ( var e in ents ) 
			{
				//	do not call Removed, this will keep statefull objects alive
				components.RemoveAllComponents( e.ID, c => sleeping.Add(c) );
				entities.Remove( e.ID );
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
			component.Added( this, entity );
		}


		public TComponent GetEntityComponent<TComponent>( Entity entity ) where TComponent: IComponent
		{
			return components.GetComponent<TComponent>( entity.ID );
		}


		public void RemoveEntityComponent( Entity entity, IComponent component )
		{
			if (entity==null) throw new ArgumentNullException("entity");
			if (component==null) throw new ArgumentNullException("component");

			component.Removed( this );
			components.RemoveComponent( entity.ID, component );
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
				.ToArray(); 
		}

	}
}
