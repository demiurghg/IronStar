using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;

namespace IronStar.ECS
{
	class ComponentCollection : Dictionary<Type,Dictionary<uint,IComponent>>
	{
		class IDComparer : IEqualityComparer<uint>
		{
			public bool Equals( uint x, uint y ) { return x==y; }
			public int GetHashCode( uint x ) { return (int)HashFunc.Hash(x); }
		}


		public ComponentCollection () : base(64)
		{
			foreach ( var componentType in Misc.GatherInterfaceImplementations( typeof(IComponent) ) )
			{
				Add( componentType, new Dictionary<uint, IComponent>(128) );
			}
		}


		public void AddComponent( uint entityId, IComponent component )
		{
			if (component==null) throw new ArgumentNullException("component");
			if (entityId==0) throw new ArgumentNullException("entityId");

			this[component.GetType()].Add( entityId, component );
		}


		public void RemoveComponent( uint entityId, IComponent component )
		{
			if (component==null) throw new ArgumentNullException("component");
			if (entityId==0) throw new ArgumentNullException("entityId");

			this[component.GetType()].Remove( entityId );
		}


		public void RemoveAllComponents( uint entityId, Action<IComponent> action )
		{
			foreach ( var dict in this )
			{
				IComponent component;
				if ( dict.Value.TryGetValue( entityId, out component ) )
				{
					dict.Value.Remove( entityId );
					action( component );
				}
			}
		}


		public void ClearComponentsOfType<TComponent>()
		{
			this[typeof(TComponent)].Clear();
		}


		public TComponent GetComponent<TComponent>( uint entityId ) where TComponent: IComponent
		{
			IComponent result;

			if ( this[typeof(TComponent)].TryGetValue( entityId, out result ) )
			{
				return (TComponent)result;
			}
			else
			{
				return default(TComponent);
			}
		}
	}
}
