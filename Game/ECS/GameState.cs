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
		GameServiceContainer Services { get { return services; } }


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
			foreach ( var e in spawned )
			{
				entities.Add( e.Id, e );
			}

			foreach ( var system in systems )
			{
				system.Update( this, gameTime );
			}

			foreach ( var id in killed )
			{
				KillInternal(id);
			}
		}


		/*-----------------------------------------------------------------------------------------------
		 *	Entity stuff :
		-----------------------------------------------------------------------------------------------*/

		public Entity Spawn ( Vector3 position, Quaternion rotation )
		{
			var entity = new Entity( this, IdGenerator.Next(), position, rotation );
			
			spawned.Add( entity );

			return entity;
		}


		public Entity Spawn ( Vector3 position )
		{
			return Spawn( position, Quaternion.Identity );
		}


		public void Kill ( uint id )
		{
			killed.Add( id );
		}


		void KillInternal( uint id )
		{
			entities.Remove( id );
			components.RemoveAllComponents( id );
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Component stuff :
		-----------------------------------------------------------------------------------------------*/

		public void AddEntityComponent ( Entity entity, IComponent component )
		{
			if (entity==null) throw new ArgumentNullException("entity");
			if (component==null) throw new ArgumentNullException("component");

			components.AddComponent( entity.Id, component );
		}


		public TComponent GetEntityComponent<TComponent>( Entity entity ) where TComponent: IComponent
		{
			return components.GetComponent<TComponent>( entity.Id );
		}


		public void RemoveEntityComponent( Entity entity, IComponent component )
		{
			if (component==null) throw new ArgumentNullException("component");

			components.RemoveComponent( entity.Id, component );
		}

		/*-----------------------------------------------------------------------------------------------
		 *	System stuff :
		-----------------------------------------------------------------------------------------------*/

		public void AddSystem ( ISystem system )
		{
			if (system==null) throw new ArgumentNullException("system");

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
				.Intersect( s2, entityComponentComparer )
				.Select( keyValue => entities[ keyValue.Key ] )
				.ToArray(); 
		}

	}
}
