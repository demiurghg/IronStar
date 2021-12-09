using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Extensions
{
	public static class SortingExtensions
	{
		/// <summary>
		/// https://www.csharp411.com/c-stable-sort/
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="comparison"></param>
		public static void InsertionSort<T>( this IList<T> list, Comparison<T> comparison )
		{
			if (list == null)
				throw new ArgumentNullException( "list" );
			if (comparison == null)
				throw new ArgumentNullException( "comparison" );

			int count = list.Count;
			for (int j = 1; j < count; j++)
			{
				T key = list[j];

				int i = j - 1;
				for (; i >= 0 && comparison( list[i], key ) > 0; i--)
				{
					list[i + 1] = list[i];
				}
				list[i + 1] = key;
			}
		}
	}
}
