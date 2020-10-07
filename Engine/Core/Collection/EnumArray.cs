using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Collection
{
	/// <summary>
	/// https://stackoverflow.com/questions/981776/using-an-enum-as-an-array-index-in-c-sharp
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="U"></typeparam>
	public class EnumArray<TIndex,TValue> : IEnumerable<TValue>
	{
		private readonly TValue[] array;
		private readonly int lower;
		private readonly int upper;

		public EnumArray()
		{
			if (!typeof(TIndex).IsEnum)
			{
				throw new InvalidOperationException("TIndex must be enum type");
			}

			lower	=	Convert.ToInt32(Enum.GetValues(typeof(TIndex)).Cast<TIndex>().Min());
			upper	=	Convert.ToInt32(Enum.GetValues(typeof(TIndex)).Cast<TIndex>().Max());
			array	=	new TValue[1 + upper - lower];
		}

		public TValue this[TIndex key]
		{
			get { return array[Convert.ToInt32(key) - lower]; }
			set { array[Convert.ToInt32(key) - lower] = value; }
		}

		public IEnumerator<TValue> GetEnumerator()
		{
			return ( (IEnumerable<TValue>)array ).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (IEnumerable<TValue>)array ).GetEnumerator();
		}
	}
}
