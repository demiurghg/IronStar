using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Mathematics
{
	/// <summary>
	/// http://graphics.stanford.edu/~seander/bithacks.html
	/// </summary>
	public static class BitUtils
	{

		/// <summary>
		/// Whether x is power of two
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		static public bool IsPowerOfTwo ( int x ) 
		{  
			return ((x & (x - 1)) == 0) && (x!=0); 
		}

		/// <summary>
		/// Finds integer log base 2 of an integer (aka the position of the highest bit set)
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		static public int LogBase2 ( int x ) 
		{  
			int v = x;	// 32-bit word to find the log base 2 of
			int r = 0;	// r will be lg(v)

			while ((v>>=1)!=0) 
			{
				r++;
			}
			return r;
		}


		/// <summary>
		/// Finds integer log base 2 of an integer (aka the position of the highest bit set)
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		static public int LogBase2 ( ulong x ) 
		{  
			ulong v = x;	// 32-bit word to find the log base 2 of
			int r = 0;			// r will be lg(v)

			while ((v>>=1)!=0) 
			{
				r++;
			}
			return r;
		}


		/// <summary>
		/// http://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		static public int RoundUpNextPowerOf2 ( int v )
		{
			v--;
			v |= v >> 1;
			v |= v >> 2;
			v |= v >> 4;
			v |= v >> 8;
			v |= v >> 16;
			v++;
			return v;
		}



		public static int CountLeadingZeros(uint i) 
		{
			int r = 0;
			const ulong mask = 1u << 31;
			while ( (i & mask) == 0 && r<32 )
			{
				r++;
				i = i << 1;
			}
			return r;
		}


		public static int CountLeadingZeros(ulong i) 
		{
			int r = 0;
			const ulong mask = 1ul << 63;
			while ( (i & mask) == 0 && r<64 )
			{
				r++;
				i = i << 1;
			}
			return r;
		}
	}
}
