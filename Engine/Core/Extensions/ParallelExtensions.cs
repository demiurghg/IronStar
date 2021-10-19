using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUutilities.Threading;

namespace Fusion.Core.Extensions {

	public static class ParallelExtensions 
	{
		public static void ForEach<T>(this IParallelLooper looper, IList<T> data, Action<T> action)
		{
			looper.ForLoop( 0, data.Count, idx => action(data[idx]) );
		}
	}
}
