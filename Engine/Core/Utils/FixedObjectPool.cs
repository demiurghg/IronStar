using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Utils {

	/// <summary>
	/// https://docs.microsoft.com/ru-ru/dotnet/standard/collections/thread-safe/how-to-create-an-object-pool
	/// </summary>
	/// <typeparam name="T"></typeparam>
    public class FixedObjectPool<T> {

        private ConcurrentBag<T> pool;
        
		public FixedObjectPool( IEnumerable<T> initialData )
        {
            pool = new ConcurrentBag<T>();

			foreach ( var item in initialData ) {
				pool.Add( item );
			}
        }


        public T Alloc()
        {
            T item;

            if (pool.TryTake(out item)) {
				return item;
			}

            throw new OutOfMemoryException("No enough elements in FixedObjectPool");
        }


        public void Recycle(T item)
        {
            pool.Add(item);
        }


		public int Count {
			get {
				return pool.Count;
			}
		}
    }
}
