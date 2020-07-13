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

		readonly FixedList<Type> componentTypes;

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

			componentTypes	=	new FixedList<Type>( (int)BitSet.MaxBits );

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
			var entity = new Entity( IdGenerator.Next(), position, rotation );
			
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

			foreach ( var componentType in components )
			{
				componentType.RemoveAll( component => component.OwnerId == id );
			}
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Component stuff :
		-----------------------------------------------------------------------------------------------*/

		int RegisterComponentType( Type componentType )
		{
			int index = componentTypes.IndexOf( componentType );

			if (index<0)
			{
				index = componentTypes.Add( componentType ); 
			}

			return index;
		}

		public bool Attach ( Entity entity, Component component )
		{
			if (entity==null) throw new ArgumentNullException("entity");
			if (component==null) throw new ArgumentNullException("component");

			int typeId = RegisterComponentType( component.GetType() );

			if (entity.Mapping[typeId])
			{
				Log.Warning("Component type '{0}' is already attached to entity #{1}", component.GetType(), entity.Id );
				return false;
			}
			else
			{
				entity.Mapping[typeId]	= true;
				component.OwnerId	= entity.Id;
				components[typeId].Add( component );
				return true;
			}
		}


		public bool Detach( Component component )
		{
			if (component==null) throw new ArgumentNullException("component");

			int typeId		=	RegisterComponentType( component.GetType() );
			var entity	=	entities[ component.OwnerId ];
			
			entity.Mapping[typeId]	=	false;

			component.OwnerId = 0;
			return components[typeId].Remove( component );
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
		 *	Queries :
		-----------------------------------------------------------------------------------------------*/

		BitSet CreateMask( Type t1, Type t2, Type t3 )
		{
			int bit1	=	componentTypes.IndexOf( t1 );
			int bit2	=	componentTypes.IndexOf( t2 );
			int bit3	=	componentTypes.IndexOf( t3 );

			var result	=	new BitSet();

			if (bit1>=0) result[bit1] = true;
			if (bit2>=0) result[bit2] = true;
			if (bit3>=0) result[bit3] = true;

			return result;
		}

		public struct Query<TComponent>
		{
			public Entity Entity;
			public TComponent Component; 
		}

		public struct Query<TComponent1,TComponent2>
		{
			public Entity Entity;
			public TComponent1 Component1; 
			public TComponent2 Component2; 
		}

		public struct Query<TComponent1,TComponent2,TComponent3>
		{
			public Entity Entity;
			public TComponent1 Component1; 
			public TComponent2 Component2; 
			public TComponent3 Component3; 
		}

		public Bag<Query<TComponent>> Gather<TComponent>()
		{
			throw new NotImplementedException();
			var bag = new Bag<Query<TComponent>>();

			foreach ( var e in entities )
			{
				
			}
		}

	}
}
