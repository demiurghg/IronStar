using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;

namespace IronStar.ECS
{
	class ComponentCollection : Dictionary<Type,ComponentBuffer>
	{
		class IDComparer : IEqualityComparer<uint>
		{
			public bool Equals( uint x, uint y ) { return x==y; }
			public int GetHashCode( uint x ) { return (int)HashFunc.Hash(x); }
		}


		public ComponentCollection () : base(64)
		{
			foreach ( var componentType in ECSTypeManager.GetComponentTypes() )
			{
				Add( componentType, new ComponentBuffer() );
			}
		}


		public void AddComponent( uint entityId, IComponent component )
		{
			if (component==null) throw new ArgumentNullException("component");
			if (entityId==0) throw new ArgumentNullException("entityId");

			//var targetDict = this[component.GetType()];
			
			//if (!targetDict.ContainsKey(entityId))
			//{
				this[component.GetType()].Add( entityId, component );
			//}
		}


		public void RemoveComponent( uint entityId, IComponent component )
		{
			if (component==null) throw new ArgumentNullException("component");
			if (entityId==0) throw new ArgumentNullException("entityId");

			this[component.GetType()].Remove( entityId );
		}


		public void RemoveAllComponents( uint entityId )
		{
			foreach ( var dict in this )
			{
				dict.Value.Remove( entityId );
			}
		}


		public void ClearComponentsOfType(Type componentType)
		{
			this[componentType].Clear();
		}


		public IComponent GetComponent( uint entityId, Type componentType )
		{
			IComponent result;

			if ( this[componentType].TryGetValue( entityId, out result ) )
			{
				return result;
			}
			else
			{
				return null;
			}
		}
	}
}
