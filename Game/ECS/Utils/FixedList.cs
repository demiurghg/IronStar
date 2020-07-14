using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	public class FixedList<T> : IEnumerable<T> 
	{
		readonly int capacity;
		readonly T[] array;
		int count;


		public FixedList(int capacity)
		{
			this.capacity	=	capacity;
			this.array		=	new T[ capacity ];
		}


		public FixedList(int capacity, IEnumerable<T> source)
		{
			this.capacity	=	capacity;
			this.array		=	new T[ capacity ];

			foreach ( var item in source.Take(capacity) )
			{
				Add( item );
			}
		}


		public int Count 
		{
			get { return count; }
		}


		public int Capacity
		{
			get { return capacity; }
		}


		public int Add ( T value )
		{
			if (count>=capacity) 
			{	
				throw new InvalidOperationException("Fixed list capacity is exceeded. Capacity is " + Capacity.ToString());
			}

			array[count] = value;

			return count++;
		}


		public int IndexOf( T item )
		{
			for (int index=0; index<Count; index++)
			{
				if (Equals(item, array[index])) return index;
			}
			return -1;
		}


		public T this[int index]
		{
			get
			{
				if (index<0 || index>=Count) throw new ArgumentOutOfRangeException("index");
				return array[index];
			}

			set
			{
				if (index<0 || index>=Count) throw new ArgumentOutOfRangeException("index");
				array[index] = value;
			}
		}

		
		public IEnumerator<T> GetEnumerator()
		{
			return ( (IEnumerable<T>)array ).GetEnumerator();
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (IEnumerable<T>)array ).GetEnumerator();
		}
	}
}
