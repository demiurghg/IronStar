using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	public static class IdGenerator
	{
		static int counter = 1;
	
		public static uint Next()
		{
			if (counter>int.MaxValue-1000) throw new ArgumentOutOfRangeException();
			return (uint)Interlocked.Increment( ref counter );
		}
	}
}
