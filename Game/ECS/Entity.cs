using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Mathematics;
using IronStar.Gameplay.Components;

namespace IronStar.ECS
{
	public sealed class Entity
	{
		public readonly GameState gs;

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
		/// Removes component from given entity
		/// </summary>
		public void RemoveComponent<TComponent>() where TComponent: IComponent
		{
			var component = GetComponent<TComponent>();
			if (component!=null)
			{
				RemoveComponent(component);
			}
			else
			{
				Log.Warning("RemoveComponent: entity {0} does not have component of type {1}", ID, typeof(TComponent) );
			}
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


		public string Name 
		{
			get { return GetComponent<NameComponent>()?.Name; }
		}
	}
}
