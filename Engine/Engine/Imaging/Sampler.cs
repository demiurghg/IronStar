using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Engine.Imaging
{
	class Sampler
	{
		public static int Clamp ( int x, int min, int max ) 
		{
			if (x < min) return min;
			if (x > max) return max;
			return x;
		}


		public static int Wrap ( int x, int wrapSize ) 
		{
			if ( x<0 ) {
				x = x % wrapSize + wrapSize;
			}
			return	x % wrapSize;
		}


		public static float Frac ( float x )
		{
			return x < 0 ? x%1+1 : x%1;
		}


		public static float Lerp ( float a, float b, float x ) 
		{
			return a*(1-x) + b*x;
		}
	}
}
