using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.BTCore
{
	public sealed class Blackboard
	{
		readonly Dictionary<string,object> dictionary = new Dictionary<string, object>();

		
		public void SetEntry<T>( string key, T value )
		{
			if (key==null) throw new ArgumentNullException(nameof(key));

			if (!dictionary.ContainsKey(key))
			{
				dictionary.Add( key, value );
			}
			else
			{
				dictionary[key] = value;
			}
		}


		public bool TryGet<T>( string key, out T value )
		{
			if (key==null) throw new ArgumentNullException(nameof(key));

			value = default(T);
			object obj;

			if (dictionary.TryGetValue(key, out obj))
			{
				if (obj.GetType()==typeof(T))
				{
					value = (T)obj;
					return true;	
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}


		public T Get<T>( string key )
		{
			T value;

			if (TryGet<T>(key, out value))
			{
				return value;
			}
			else
			{
				return default(T);
			}
		}



	}
}
