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
		public readonly uint ID;

		internal long ComponentMapping;
		internal long SystemMapping;

		/// <summary>
		/// Entity constructor
		/// </summary>
		/// <param name="id"></param>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		public Entity ( GameState gs, uint id )
		{
			this.gs			=	gs;
			this.ID			=	id;
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
		/// Indicates that given entity containts component of given type.
		/// </summary>
		/// <typeparam name="TComponent"></typeparam>
		/// <returns></returns>
		public bool ContainsComponent<TComponent>() where TComponent: IComponent
		{
			return GetComponent<TComponent>() != null;
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
