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
		static int counter = 0;
	
		public static uint Next()
		{
			return (uint)Interlocked.Increment( ref counter );
		}
	}
}
