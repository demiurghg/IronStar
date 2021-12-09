﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Mathematics;
using IronStar.Gameplay.Components;

namespace IronStar.ECS
{
	public enum Domain : byte
	{
		Simulation, 
		Presentation,
		Server,
		Client,
	}

	public sealed class Entity
	{
		public readonly GameState gs;

		/// <summary>
		/// Unique entity ID
		/// </summary>
		public readonly uint ID;

		internal long ComponentMapping;
		internal long SystemMapping;

		public object Tag;

		public Domain Domain { get { return (Domain)(ID >> 28); } }

		public bool IsLocalDomain { get { return gs.Domain==Domain; } }

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
		/// Creates copy of given entity keeping old mapping bits
		/// </summary>
		/// <returns></returns>
		internal Entity MakeCopyInternal()
		{
			var e = new Entity(gs, ID);
			e.ComponentMapping	=	ComponentMapping;
			e.SystemMapping		=	SystemMapping;
			return e;
		}
		
		
		/// <summary>
		/// Kills this entity
		/// </summary>
		public void Kill()
		{
			gs.Kill(this);
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
		/*public void RemoveComponent( IComponent component )
		{
			gs.RemoveEntityComponent( this, component );
		}*/


		/// <summary>
		/// Removes component from given entity
		/// </summary>
		public void RemoveComponent<TComponent>() where TComponent: IComponent
		{
			gs.RemoveEntityComponent( this, typeof(TComponent) );
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
			return (TComponent)GetComponent(typeof(TComponent));
		}


		/// <summary>
		/// Gets entity's component by its index and type
		/// </summary>
		/// <typeparam name="TComponent">Component type</typeparam>
		/// <returns>Entity's component</returns>
		public IComponent GetComponent(Type componentType)
		{
			return gs.GetEntityComponent(this, componentType);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendFormat("#{0}:", ID);
			
			for (int shl=0; shl<64; shl++) 
			{
				long bit = ((long)1) << shl;

				if ((bit & ComponentMapping) != 0)
				{
					var type = ECSTypeManager.GetComponentType( 1u << shl );
					//var comp = gs.GetEntityComponent(this, type);
					var name = type.Name.Replace("Component","");
					sb.AppendFormat("[" + name + "]");
				}
			}

			return sb.ToString();
		}


		public IComponent[] DebugComponentList
		{
			get 
			{	
				var list = new List<IComponent>();

				for (int shl=0; shl<64; shl++) 
				{
					long bit = ((long)1) << shl;

					if ((bit & ComponentMapping) != 0)
					{
						var type = ECSTypeManager.GetComponentType( 1u << shl );
						var comp = gs.GetEntityComponent(this, type);
						list.Add(comp);
					}
				}

				return list.ToArray();
			}
		}


		public Vector3 Location
		{
			get 
			{
				var transform = GetComponent<Transform>();
				return (transform==null) ? Vector3.Zero : transform.Position;
			}
		}
	}
}
