using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Core.Extensions {

	public static class LinqExtensions {
		
		public static T SelectMaxOrDefault<T>(this IEnumerable<T> list, Func<T, float> selector)
		{
			if (!list.Any()) return default(T);
			return list.Aggregate((acc, next) => (selector(acc) > selector(next)) ? acc : next);
		}

		
		public static bool SelectMaxOrDefault<T>(this IEnumerable<T> list, Func<T, float> selector, out T result )
		{
			result = default(T);
			if (!list.Any()) return false;
			result = list.Aggregate((acc, next) => (selector(acc) > selector(next)) ? acc : next);
			return true;
		}


		public static int IndexOfMaximum<T>( this IEnumerable<T> list, Func<T,float> selector )
		{
			var maximum	=	float.MinValue;
			var index	=	-1;
			for (int i=0; i<list.Count(); i++) {
				var value = selector( list.ElementAt(i) );
				if (maximum<value) {
					maximum = value;
					index = i;
				}
			}
			return index;
		}


		public static T SelectMinOrDefault<T>(this IEnumerable<T> list, Func<T, float> selector)
		{
			if (!list.Any()) return default(T);
			return list.Aggregate((acc, next) => (selector(acc) < selector(next)) ? acc : next);
		}


		public static IEnumerable<TSource> DistinctBy<TSource, TKey> (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			HashSet<TKey> seenKeys = new HashSet<TKey>();
			foreach (TSource element in source)
			{
				if (seenKeys.Add(keySelector(element)))
				{
					yield return element;
				}
			}
		}

		public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count)
		{
			return source.Skip(Math.Max(0, source.Count() - count));
		}


		/// <summary>
		/// https://stackoverflow.com/questions/5729572/eliminate-consecutive-duplicates-of-list-elements
		/// https://stackoverflow.com/a/5729869
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public static IEnumerable<T> DistinctAdjacent<T>(this IEnumerable<T> source)
		{
			List<T> results = new List<T>();

			foreach (var element in source) {
				if (results.Count == 0 || !results.Last().Equals(element) ) {
					results.Add(element);
				}
			}

			return results;
		}


		/// <summary>
		/// https://www.codeproject.com/Tips/494499/Implementing-Dictionary-RemoveAll
		/// </summary>
		/// <typeparam name="K"></typeparam>
		/// <typeparam name="V"></typeparam>
		/// <param name="dict"></param>
		/// <param name="match"></param>
		public static void RemoveAll<K, V>(this IDictionary<K, V> dict, Func<K, V, bool> match)
		{
			foreach (var key in dict.Keys.ToArray()
					.Where(key => match(key, dict[key])))
				dict.Remove(key);
		} 


		/// <summary>
		/// https://stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="rand"></param>
		/// <returns></returns>
		public static IEnumerable<T> Shuffle<T>( this IEnumerable<T> source, Random rand )
		{
			return source.OrderBy(x => rand.Next());
		}


		/// <summary>
		/// Gets random value from source
		/// </summary>
		public static T RandomOrDefault<T>( this IEnumerable<T> source, Random rand )
		{
			var num = source.Count();

			return num==0 ? default(T) : source.ElementAt( rand.Next(0, num) );
		}
	}
}
