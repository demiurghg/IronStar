using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Widgets.Binding
{
	public class ListBinding : IListBinding
	{
		object[] array;

		public ListBinding( IEnumerable<object> items )
		{
			array = items.ToArray();
		}

		public object this[int index]
		{
			get
			{
				return array[index];
			}
		}

		public int Count
		{
			get
			{
				return array.Length;
			}
		}

		public bool IsReadonly
		{
			get
			{
				return true;
			}
		}

		public void Add( object item )
		{
			throw new InvalidOperationException();
		}

		public void Remove( object item )
		{
			throw new InvalidOperationException();
		}
	}
}
