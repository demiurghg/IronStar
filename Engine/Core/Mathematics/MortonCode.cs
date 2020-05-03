using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Mathematics
{
	/// <summary>
	/// http://asgerhoedt.dk/?p=276
	/// </summary>
	public static class MortonCode
	{
		static uint SeparateBy1(uint x) 
		{
			x &= 0x0000ffff;                  // x = ---- ---- ---- ---- fedc ba98 7654 3210
			x = (x ^ (x <<  8)) & 0x00ff00ff; // x = ---- ---- fedc ba98 ---- ---- 7654 3210
			x = (x ^ (x <<  4)) & 0x0f0f0f0f; // x = ---- fedc ---- ba98 ---- 7654 ---- 3210
			x = (x ^ (x <<  2)) & 0x33333333; // x = --fe --dc --ba --98 --76 --54 --32 --10
			x = (x ^ (x <<  1)) & 0x55555555; // x = -f-e -d-c -b-a -9-8 -7-6 -5-4 -3-2 -1-0
			return x;
		}		

		static uint CompactBy1(uint x) 
		{
			x &= 0x55555555;                  // x = -f-e -d-c -b-a -9-8 -7-6 -5-4 -3-2 -1-0
			x = (x ^ (x >>  1)) & 0x33333333; // x = --fe --dc --ba --98 --76 --54 --32 --10
			x = (x ^ (x >>  2)) & 0x0f0f0f0f; // x = ---- fedc ---- ba98 ---- 7654 ---- 3210
			x = (x ^ (x >>  4)) & 0x00ff00ff; // x = ---- ---- fedc ba98 ---- ---- 7654 3210
			x = (x ^ (x >>  8)) & 0x0000ffff; // x = ---- ---- ---- ---- fedc ba98 7654 3210
			return x;
		}

		public static uint Code2(Int2 xy) 
		{
			uint x = (uint)xy.X;
			uint y = (uint)xy.Y;
			return SeparateBy1(x) | (SeparateBy1(y) << 1);
		}

		public static Int2 Decode2(uint c) 
		{
			uint x = CompactBy1(c);
			uint y = CompactBy1(c >> 1);
			return new Int2( (int)x, (int)y );
		}
	}
}
