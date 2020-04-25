using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Engine.Imaging
{
	class Sampler
	{


		public static float Lerp ( float a, float b, float x ) 
		{
			return a*(1-x) + b*x;
		}
	}
}
