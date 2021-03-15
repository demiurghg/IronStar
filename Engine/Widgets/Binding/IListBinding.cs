using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Widgets.Binding
{
	public interface IListBinding
	{
		/// <summary>
		/// Indicates that list is read only, 
		/// Add and Remove should not be used.
		/// </summary>
		bool IsReadonly { get; }

		/// <summary>
		/// Adds item to the list
		/// </summary>
		/// <param name="item"></param>
		void Add( object item );

		/// <summary>
		/// Removed item from the list
		/// </summary>
		/// <param name="item"></param>
		void Remove( object item );

		/// <summary>
		/// Gets item by index
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		object this[int index] { get; }

		/// <summary>
		/// Gets number of item
		/// </summary>
		int Count { get; }
	}
}
