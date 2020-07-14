using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	public sealed class Entity
	{
		readonly GameState gs;

		/// <summary>
		/// Unique entity ID
		/// </summary>
		public readonly uint Id;

		/// <summary>
		/// Entity position :
		/// </summary>
		public Vector3	Position;

		/// <summary>
		/// Entity rotation
		/// </summary>
		public Quaternion Rotation;

		/// <summary>
		/// Bit mask indicating which system process given entity
		/// </summary>
		internal BitSet Mapping;

		/// <summary>
		/// Bit mask indicating general entity states, like static, save/load, network, etc.
		/// </summary>
		public BitSet EntityState;

		/// <summary>
		/// Entity constructor
		/// </summary>
		/// <param name="id"></param>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		public Entity ( GameState gs, uint id, Vector3 position, Quaternion rotation )
		{
			this.gs			=	gs;
			this.Id			=	id;
			this.Mapping	=	new BitSet(0);
			this.Position	=	position;
			this.Rotation	=	rotation;
		}

		/// <summary>
		/// Entity constructor
		/// </summary>
		public Entity ( GameState gs, uint id, Vector3 position ) : this( gs, id, position, Quaternion.Identity )
		{
		}

		/// <summary>
		/// Entity constructor
		/// </summary>
		public Entity ( GameState gs, uint id ) : this( gs, id, Vector3.Zero, Quaternion.Identity )
		{
		}


		/// <summary>
		/// Adds component to given entity
		/// </summary>
		public void AddComponent( IComponent component )
		{
			gs.AddEntityComponent( this, component );
		}


		/// <summary>
		/// Removes component from given entity
		/// </summary>
		public void RemoveComponent( IComponent component )
		{
			gs.RemoveEntityComponent( this, component );
		}


		/// <summary>
		/// Gets entity's component by its index and type
		/// </summary>
		/// <typeparam name="TComponent">Component type</typeparam>
		/// <returns>Entity's component</returns>
		public TComponent GetComponent<TComponent>() where TComponent: IComponent
		{
			return gs.GetEntityComponent<TComponent>(this);
		}
	}
}
