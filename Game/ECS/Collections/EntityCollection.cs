using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	class EntityCollection
	{
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


		const int InitialCapacity = 1024;
		readonly object lockObj = new object();
		readonly Dictionary<uint,Entity> dict;


		public EntityCollection()
		{
			dict = new Dictionary<uint, Entity>( InitialCapacity, new IDComparer() );
		}


		public void Add( Entity e )
		{
			lock (lockObj)
			{
				dict.Add( e.ID, e );
			}
		}


		public bool Remove( Entity e )
		{
			lock (lockObj)
			{
				return dict.Remove(e.ID);
			}
		}


		public Entity[] GetSnapshot()
		{
			lock (lockObj)
			{
				return dict.Values.ToArray();
			}
		}


		public bool Contains( uint id )
		{
			return dict.ContainsKey( id );
		}


		public Entity[] Query( Aspect aspect )
		{
			lock (lockObj)
			{
				return dict.Values
					.Where( e => aspect.Accept(e) )
					.ToArray()
					;
				}
		}


		public int Count
		{
			get { return dict.Count; }
		}


		public Entity this[uint key]
		{
			get 
			{
				Entity e;
				if ( dict.TryGetValue(key, out e) )
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
				dict[key] = value;
			}
		}
	}
}
