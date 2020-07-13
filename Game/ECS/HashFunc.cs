using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	/// <summary>
	///	https://stackoverflow.com/questions/664014/what-integer-hash-function-are-good-that-accepts-an-integer-hash-key
	/// </summary>
	public static class HashFunc
	{
		public static uint Hash(uint x) 
		{
			x = ((x >> 16) ^ x) * 0x45d9f3b;
			x = ((x >> 16) ^ x) * 0x45d9f3b;
			x = (x >> 16) ^ x;
			return x;
		}

		public static uint Unhash(uint x) 
		{
			x = ((x >> 16) ^ x) * 0x119de1f3;
			x = ((x >> 16) ^ x) * 0x119de1f3;
			x = (x >> 16) ^ x;
			return x;
		}
	}
}
