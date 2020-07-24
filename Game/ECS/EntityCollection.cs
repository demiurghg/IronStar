using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	class EntityCollection : Dictionary<uint, Entity>
	{
		const int InitialCapacity = 1024;

		class IDComparer : IEqualityComparer<uint>
		{
			public bool Equals( uint x, uint y )
			{
				return x == y;
			}

			public int GetHashCode( uint x )
			{
				return (int)HashFunc.Hash(x); 
			}
		}

		public EntityCollection() : base(InitialCapacity, new IDComparer())
		{
		}


		public new Entity this[uint key]
		{
			get 
			{
				Entity e;
				if ( TryGetValue(key, out e) )
				{
					return e;
				}
				else
				{
					return null;
				}
			}

			set 
			{
				base[key] = value;
			}
		}
	}
}
