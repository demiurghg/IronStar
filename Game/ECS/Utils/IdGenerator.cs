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

		const uint MaxId = 0x0FFFFFFF;
	
		public static uint Next(Domain domain)
		{
			if (counter>MaxId-1000) 
			{
				throw new ArgumentOutOfRangeException();
			}

			uint domainBits = (((uint)(domain)) << 28);

			return (uint)Interlocked.Increment( ref counter ) | domainBits;
		}
	}
}
