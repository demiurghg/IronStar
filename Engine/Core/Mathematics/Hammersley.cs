using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Mathematics {

	/// <summary>
	/// http://holger.dammertz.org/stuff/notes_HammersleyOnHemisphere.html#sec-SourceCode
	/// </summary>
	public static class Hammersley {

		static float RadicalInverseVdC(Int64 bits) 
		{
			bits = (bits << 16) | (bits >> 16);
			bits = ((bits & 0x55555555) << 1) | ((bits & 0xAAAAAAAA) >> 1);
			bits = ((bits & 0x33333333) << 2) | ((bits & 0xCCCCCCCC) >> 2);
			bits = ((bits & 0x0F0F0F0F) << 4) | ((bits & 0xF0F0F0F0) >> 4);
			bits = ((bits & 0x00FF00FF) << 8) | ((bits & 0xFF00FF00) >> 8);
			return (float)((bits) * 2.3283064365386963e-10); // / 0x100000000
		}		

		
		public static Vector2 Hammersley2D(int i, int N) 
		{
		    return new Vector2( i/(float)N , RadicalInverseVdC(i));
		}

		
		static float cos(float x) { return (float)Math.Cos(x); }
		static float sin(float x) { return (float)Math.Sin(x); }
		static float sqrt(float x) { return (float)Math.Sqrt(x); }

		const float PI = 3.14159265358979f;


		public static Vector3 SphereUniform(float u, float v) 
		{
			float phi = v * 2.0f * PI;
			float cosTheta = 1.0f - 2*u;
			float sinTheta = sqrt(1.0f - cosTheta * cosTheta);
			return new Vector3(cos(phi) * sinTheta, cosTheta, sin(phi) * sinTheta);
		}
    

		public static Vector3 HemisphereUniform(float u, float v) 
		{
			float phi = v * 2.0f * PI;
			float cosTheta = 1.0f - u;
			float sinTheta = sqrt(1.0f - cosTheta * cosTheta);
			return new Vector3(cos(phi) * sinTheta, cosTheta, sin(phi) * sinTheta);
		}
    

		public static Vector3 HemisphereCosine(float u, float v) 
		{
			float phi = v * 2.0f * PI;
			float cosTheta = sqrt(1.0f - u);
			float sinTheta = sqrt(1.0f - cosTheta * cosTheta);
			return new Vector3(cos(phi) * sinTheta, cosTheta, sin(phi) * sinTheta);
		}


		public static Vector3 SphereUniform(int i, int n) 
		{
			var xy = Hammersley2D(i, n);
			return SphereUniform( xy.X, xy.Y );
		}
    

		public static Vector3 HemisphereUniform(int i, int n) 
		{
			var xy = Hammersley2D(i, n);
			return HemisphereUniform( xy.X, xy.Y );
		}
    

		public static Vector3 HemisphereCosine(int i, int n) 
		{
			var xy = Hammersley2D(i, n);
			return HemisphereCosine( xy.X, xy.Y );
		}
    

 	}
}
