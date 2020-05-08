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
		/*-----------------------------------------------------------------------------------------
		 *	2-dimensional
		-----------------------------------------------------------------------------------------*/
		
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


		/*-----------------------------------------------------------------------------------------
		 *	3-dimensional
		-----------------------------------------------------------------------------------------*/
		
		static uint SeparateBy2(uint x) 
		{
			x &= 0x03ff;						//	0000 0000 0000 0000 0000 0011 1111 1111
			x = (x | (x << 16)) & 0x030000FF;	//	0000 0011 0000 0000 0000 0000 1111 1111
			x = (x | (x <<  8)) & 0x0300F00F;	//	0000 0011 0000 0000 1111 0000 0000 1111
			x = (x | (x <<  4)) & 0x030C30C3;	//	0000 0011 0000 1100 0011 0000 1100 0011
			x = (x | (x <<  2)) & 0x09249249; 	//	0000 1001 0010 0100	1001 0010 0100 1001
			return x;
		}		

		static uint CompactBy2(uint x) 
		{	
										        
			x &=                  0x09249249;	// 0000 1001 0010 0100 1001 0010 0100 1001
			x = (x ^ (x >> 2))  & 0x030C30C3;	// 0000 0011 0000 1100 0011 0000 1100 0011
			x = (x ^ (x >> 4))  & 0x0300f00f;	// 0000 0011 0000 0000 1111 0000 0000 1111
			x = (x ^ (x >> 8))  & 0x030000ff;	// 0000 0011 0000 0000 0000 0000 1111 1111
			x = (x ^ (x >> 16)) & 0x000003ff;	// 0000 0000 0000 0000 0000 0011 1111 1111

			return x;

			//	}
			//	x &= 0x55555555;                  // x = -f-e -d-c -b-a -9-8 -7-6 -5-4 -3-2 -1-0
			//x = (x ^ (x >>  1)) & 0x33333333; // x = --fe --dc --ba --98 --76 --54 --32 --10
			//x = (x ^ (x >>  2)) & 0x0f0f0f0f; // x = ---- fedc ---- ba98 ---- 7654 ---- 3210
			//x = (x ^ (x >>  4)) & 0x00ff00ff; // x = ---- ---- fedc ba98 ---- ---- 7654 3210
			//x = (x ^ (x >>  8)) & 0x0000ffff; // x = ---- ---- ---- ---- fedc ba98 7654 3210
			//return x;
		}

		public static uint Code3(Int3 xyz) 
		{
			uint x = (uint)xyz.X;
			uint y = (uint)xyz.Y;
			uint z = (uint)xyz.Z;
			return SeparateBy2(x) | (SeparateBy2(y) << 1) | (SeparateBy2(z) << 2);
		}


		public static Int3 Decode3(uint c) 
		{
			uint x = CompactBy2(c);
			uint y = CompactBy2(c >> 1);
			uint z = CompactBy2(c >> 2);
			return new Int3( (int)x, (int)y, (int)z );
		}


	}
}
