using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace IronStar.ECS
{
	public class Bag<T> : IEnumerable<T>
	{
		private T[] elements;


		public Bag(int capacity = 16)
		{
			elements = new T[capacity];
			Count = 0;
		}

		private void Grow()
		{
			this.Grow((int)(elements.Length * 1.5) + 1);
		}


		private void Grow(int newCapacity)
		{
			T[] oldElements = elements;
			elements = new T[newCapacity];
			Array.Copy(oldElements, 0, elements, 0, oldElements.Length);
		}

		/*-----------------------------------------------------------------------------------------
		 *	Properties :
		-----------------------------------------------------------------------------------------*/

		public int Capacity
		{
			get	{ return elements.Length; }
		}


		public bool IsEmpty
		{
			get { return Count == 0; }
		}


		public int Count { get; private set; }

		/*-----------------------------------------------------------------------------------------
		 *	Accessors :
		-----------------------------------------------------------------------------------------*/

		public T this[int index]
		{
			get
			{
				return elements[index];
			}

			set
			{
				if (index >= elements.Length)
				{
					Grow(index * 2);
					Count = index + 1;
				}
				else if (index >= Count)
				{
					Count = index + 1;
				}

				elements[index] = value;
			}
		}


		public void Set(int index, T element)
		{
			if (index >= elements.Length)
			{
				Grow(index * 2);
				Count = index + 1;
			}
			else if (index >= Count)
			{
				Count = index + 1;
			}

			elements[index] = element;
		}


		public T Get(int index)
		{
			return elements[index];
		}

		/*-----------------------------------------------------------------------------------------
		 *	Add/Remove/Clear stuff :
		-----------------------------------------------------------------------------------------*/

		public void Add(T element)
		{
			// is size greater than capacity increase capacity
			if (Count == elements.Length)
			{
				Grow();
			}

			elements[Count] = element;
			++Count;
		}


		public void AddRange(Bag<T> rangeOfElements)
		{
			for (int index = 0, j = rangeOfElements.Count; j > index; ++index)
			{
				Add(rangeOfElements.Get(index));
			}
		}


		public void Clear()
		{
			// Null all elements so garbage collector can clean up.
			for (int index = Count - 1; index >= 0; --index)
			{
				elements[index] = default(T);
			}

			Count = 0;
		}


		public bool Contains(T element)
		{
			for (int index = Count - 1; index >= 0; --index)
			{
				if (element.Equals(elements[index]))
				{
					return true;
				}
			}

			return false;
		}


		public T Remove(int index)
		{
			// Make copy of element to remove so it can be returned.
			T result = elements[index];
			--Count;
			
			// Overwrite item to remove with last element.
			elements[index] = elements[Count];

			// Null last element, so garbage collector can do its work.
			elements[Count] = default(T);
			return result;
		}


		public bool Remove(T element)
		{
			for (int index = Count - 1; index >= 0; --index)
			{
				if (element.Equals(elements[index]))
				{
					--Count;

					// Overwrite item to remove with last element.
					elements[index] = elements[Count];
					elements[Count] = default(T);

					return true;
				}
			}

			return false;
		}


		public int RemoveAll(Bag<T> bag)
		{
			int removed = 0;
			for (int index = bag.Count - 1; index >= 0; --index)
			{
				if (Remove(bag.Get(index)))
				{
					removed++;
				}
			}

			return removed;
		}


		public int RemoveAll(Func<T,bool> predicate)
		{
			int removed = 0;

			int index = 0;

			while (index<Count)
			{
				if (predicate(Get(index)))
				{
					Remove(index);
					removed++;
				}
				else
				{
					index++;
				}
			}

			return removed;
		}


		public T RemoveLast()
		{
			if (Count > 0)
			{
				--Count;
				T result = elements[Count];

				// default(T) if class = null.
				elements[Count] = default(T);
				return result;
			}

			return default(T);
		}


		/*-----------------------------------------------------------------------------------------
		 *	Sorting :
		-----------------------------------------------------------------------------------------*/

		public void Sort()
		{
			Array.Sort( elements, 0, Count );
		}

		public void Sort( IComparer<T> comparer )
		{
			Array.Sort( elements, 0, Count, comparer );
		}

		/*-----------------------------------------------------------------------------------------
		 *	IEnumerable :
		-----------------------------------------------------------------------------------------*/

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return new BagEnumerator<T>(this);
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			return new BagEnumerator<T>(this);
		}
	}
}
