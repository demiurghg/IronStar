using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	public class ComponentTuple
	{
		public ComponentTuple( IComponent current ) { Current = current; }
		public bool			Updated		=	false;
		public IComponent	Current		=	null;
		public IComponent	Previous	=	null;
		public IComponent	Lerped		=	null;
	}

	/// <summary>
	/// #TODO #ECS #REFACTOR -- move interpolation to separate class
	/// </summary>
	class ComponentDictionary : Dictionary<uint,ComponentTuple>
	{
		public ComponentDictionary()
		{
		}

		public void Add( uint id, IComponent c )
		{
			if (!ContainsKey(id))
			{
				Add( id, new ComponentTuple(c) );
			}
			else
			{
				Log.Warning("Entity #{0} already has component {1}", id, c.GetType().Name );
			}
		}

		
		public bool TryGetValue( uint id, out IComponent component )
		{
			ComponentTuple tuple;
			component = null;

			if (TryGetValue(id, out tuple))
			{
				component = tuple.Lerped ?? tuple.Current;
				return true;
			}
			
			return false;
		}


		public void Interpolate( float dt, float factor )
		{ 
			foreach ( var keyValue in this )
			{
				var tuple = keyValue.Value;
				
				tuple.Lerped = tuple.Current.Interpolate( tuple.Previous, dt, factor );

				/*if (tuple.Previous!=null)
				{
					tuple.Lerped = tuple.Current.Interpolate( tuple.Previous, dt, factor );
				}
				else
				{
					tuple.Lerped = tuple.Current;
				}*/
			}
		}
	}
}