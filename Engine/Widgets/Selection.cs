using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Fusion.Widgets
{
	public class Selection<T> : ICollection<T>
	{
		readonly List<T> list;

		public int Count { get { return list.Count; } }
		public bool IsReadOnly { get { return ( (ICollection<T>)list ).IsReadOnly; } }

		public event EventHandler	Changed;


		public Selection ()
		{
			this.list	=	new List<T>();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return ( (IEnumerable<T>)list ).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (IEnumerable<T>)list ).GetEnumerator();
		}

		public void Add( T item )
		{
			//	remove if already added and move to the end of the list
			if (!list.Contains(item))
			{
				list.Add( item );
				Changed?.Invoke(this, EventArgs.Empty);
			}
		}

		public void Clear()
		{
			if (list.Any())
			{
				list.Clear();
				Changed?.Invoke(this, EventArgs.Empty);
			}
		}

		public bool Remove( T item )
		{
			if (list.Remove( item )) 
			{
				Changed?.Invoke(this, EventArgs.Empty);
				return true;
			}
			return false;
		}

		public void AddRange( IEnumerable<T> items )
		{
			foreach ( var item in items )
			{
				list.Remove(item);
				list.Add( item );
			}
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public void RemoveRange( IEnumerable<T> items )
		{
			bool changed = false;

			foreach ( var item in items )
			{
				changed |= list.Remove(item);
			}

			if (changed)
			{
				Changed?.Invoke(this, EventArgs.Empty);
			}
		}

		public void SetRange( IEnumerable<T> items )
		{
			list.Clear();
			list.AddRange( items );
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public void Toggle( T item )
		{
			if (item==null) return;

			if (list.Contains(item))
			{
				list.Remove( item );
			}
			else
			{
				list.Add( item );
			}
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public bool Contains( T item )
		{
			return list.Contains( item );
		}

		public void CopyTo( T[] array, int arrayIndex )
		{
			list.CopyTo( array, arrayIndex );
		}
	}
}
