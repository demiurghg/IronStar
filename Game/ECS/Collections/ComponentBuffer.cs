using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	class ComponentBuffer : IDictionary<uint,IComponent>
	{
		Dictionary<uint,IComponent>	dict = new Dictionary<uint, IComponent>();

		public IComponent this[uint key]
		{
			get
			{
				lock(dict)
				{
					return ( (IDictionary<uint, IComponent>)dict )[key];
				}
			}

			set
			{
				lock(dict)
				{
					( (IDictionary<uint, IComponent>)dict )[key]=value;
				}
			}
		}

		public int Count
		{
			get
			{
				lock(dict)
				{
					return ( (IDictionary<uint, IComponent>)dict ).Count;
				}
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public ICollection<uint> Keys
		{
			get
			{
				lock(dict)
				{
					return ( (IDictionary<uint, IComponent>)dict ).Keys;
				}
			}
		}

		public ICollection<IComponent> Values
		{
			get
			{
				lock(dict)
				{
					return ( (IDictionary<uint, IComponent>)dict ).Values;
				}
			}
		}

		public void Add( KeyValuePair<uint, IComponent> item )
		{
			lock(dict)
			{
				( (IDictionary<uint, IComponent>)dict ).Add( item );
			}
		}

		public void Add( uint key, IComponent value )
		{
			lock(dict)
			{
				( (IDictionary<uint, IComponent>)dict ).Add( key, value );
			}
		}

		public void Clear()
		{
			lock(dict)
			{
				( (IDictionary<uint, IComponent>)dict ).Clear();
			}
		}

		public bool Contains( KeyValuePair<uint, IComponent> item )
		{
			lock(dict)
			{
				return ( (IDictionary<uint, IComponent>)dict ).Contains( item );
			}
		}

		public bool ContainsKey( uint key )
		{
			lock(dict)
			{
				return ( (IDictionary<uint, IComponent>)dict ).ContainsKey( key );
			}
		}

		public void CopyTo( KeyValuePair<uint, IComponent>[] array, int arrayIndex )
		{
			lock(dict)
			{
				( (IDictionary<uint, IComponent>)dict ).CopyTo( array, arrayIndex );
			}
		}

		public IEnumerator<KeyValuePair<uint, IComponent>> GetEnumerator()
		{
			lock(dict)
			{
				return ( (IDictionary<uint, IComponent>)dict ).GetEnumerator();
			}
		}

		public bool Remove( KeyValuePair<uint, IComponent> item )
		{
			lock(dict)
			{
				return ( (IDictionary<uint, IComponent>)dict ).Remove( item );
			}
		}

		public bool Remove( uint key )
		{
			lock(dict)
			{
				return ( (IDictionary<uint, IComponent>)dict ).Remove( key );
			}
		}

		public bool TryGetValue( uint key, out IComponent value )
		{
			lock(dict)
			{
				return ( (IDictionary<uint, IComponent>)dict ).TryGetValue( key, out value );
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			lock(dict)
			{
				return ( (IDictionary<uint, IComponent>)dict ).GetEnumerator();
			}
		}
	}
}
