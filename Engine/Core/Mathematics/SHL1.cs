using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Fusion.Core.Mathematics {

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SHL1 : IEquatable<SHL1>{
		
        public static readonly SHL1 Zero = new SHL1(0,0,0,0);

		public float A;
		public float B;
		public float C;
		public float D;

        /// <summary>
        /// Gets or sets the component at the specified index.
        /// </summary>
        public float this[int index]
        {
			get	{
				switch (index) {
					case 0:	return A;
					case 1:	return B;
					case 2:	return C;
					case 3:	return D;
				}
				throw new ArgumentOutOfRangeException("index", "Indices for SHL1 run from 0 to 3, inclusive.");
			}

			set {
				switch (index) {
					case 0:	A = value;	break;
					case 1:	B = value;	break;
					case 2:	C = value;	break;
					case 3:	D = value;	break;
					default:
						throw new ArgumentOutOfRangeException("index", "Indices for SHL1 run from 0 to 3, inclusive.");
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="l0"></param>
		/// <param name="l1"></param>
		/// <param name="l2"></param>
		/// <param name="l3"></param>
		public SHL1( float a, float b, float c, float d )
		{
			A	=	a;
			B	=	b;
			C	=	c;
			D	=	d;
		}


		/// <summary>
		/// Evaluates SH for given direction
		/// </summary>
		/// <param name="normalizedDir"></param>
		public static SHL1 Evaluate ( Vector3 normalizedDir )
		{
			var p	=	normalizedDir;
			var sh	=	new SHL1();

			var	Y0	=	0.282095f; // sqrt(1/fourPi)
			var	Y1	=	0.488603f; // sqrt(3/fourPi)

			sh[0] = Y0;
			sh[1] = Y1 * p.Y;
			sh[2] = Y1 * p.Z;
			sh[3] = Y1 * p.X;
			return sh;
		}


		/// <summary>
		/// Simplified Evaluate+DiffuseConvolution
		/// </summary>
		/// <param name="normalizedDir"></param>
		public static SHL1 EvaluateDiffuse ( Vector3 normalizedDir )
		{
			var p	=	normalizedDir;
			var sh	=	new SHL1();

			var	AY0	=	0.25f; 
			var	AY1	=	0.50f; 

			sh[0] = AY0;
			sh[1] = AY1 * p.Y;
			sh[2] = AY1 * p.Z;
			sh[3] = AY1 * p.X;
			return sh;
		}


		/// <summary>
		/// For rendering, we need to compute the amount of light that falls on a surface with a
		/// particular normal from all directions on a hemisphere around it. In other words, we
		/// need to compute irradiance at a particular point on the geometry surface.
		/// This is achived by convolving the radiance function in SH form with the SH form of our
		/// BRDF (clamped cosine).
		/// </summary>
		public SHL1 DiffuseConvolution ( SHL1 radianceSh )
		{
			var	irradianceSh	=	radianceSh;
			var	A0	=	0.886227f; // pi/sqrt(fourPi)
			var	A1	=	1.023326f; // sqrt(pi/3)
			irradianceSh[0] *= A0;
			irradianceSh[1] *= A1;
			irradianceSh[2] *= A1;
			irradianceSh[3] *= A1;
			return irradianceSh;
		}


		/// <summary>
		/// Computes value of the SH for given direction
		/// </summary>
		/// <param name="normalizedDir"></param>
		/// <returns></returns>
		public float ComputeRadiance ( Vector3 normalizedDir )
		{
			float x		=	normalizedDir.X;
			float y		=	normalizedDir.Y;
			float z		=	normalizedDir.Z;
			float L0	=	this[1]*y + this[2]*z + this[3]*x;
			float L1	=	this[0];

			return L0 + L1;
		}

		/*-----------------------------------------------------------------------------------------------
		 * 
		 *	Operators and equality
		 * 
		-----------------------------------------------------------------------------------------------*/

		#region Some neccessary copy-paste

		public static SHL1 operator +(SHL1 left, SHL1 right)
		{
			return new SHL1(left.A + right.A, left.B + right.B, left.C + right.C, left.D + right.D);
		}

		public static SHL1 operator *(SHL1 left, SHL1 right)
		{
			return new SHL1(left.A * right.A, left.B * right.B, left.C * right.C, left.D * right.D);
		}

		public static SHL1 operator +(SHL1 value)
		{
			return value;
		}

		public static SHL1 operator -(SHL1 left, SHL1 right)
		{
			return new SHL1(left.A - right.A, left.B - right.B, left.C - right.C, left.D - right.D);
		}

		public static SHL1 operator -(SHL1 value)
		{
			return new SHL1(-value.A, -value.B, -value.C, -value.D);
		}

		public static SHL1 operator *(float scale, SHL1 value)
		{
			return new SHL1(value.A * scale, value.B * scale, value.C * scale, value.D * scale);
		}

		public static SHL1 operator *(SHL1 value, float scale)
		{
			return new SHL1(value.A * scale, value.B * scale, value.C * scale, value.D * scale);
		}

		public static SHL1 operator /(SHL1 value, float scale)
		{
			return new SHL1(value.A / scale, value.B / scale, value.C / scale, value.D / scale);
		}

		public static SHL1 operator /(float scale,SHL1 value)
		{
			return new SHL1(scale / value.A, scale / value.B, scale / value.C, scale / value.D);
		}

		public static SHL1 operator /(SHL1 value, SHL1 scale)
		{
			return new SHL1(value.A / scale.A, value.B / scale.B, value.C / scale.C, value.D / scale.D);
		}

		public static SHL1 operator +(SHL1 value, float scalar)
		{
			return new SHL1(value.A + scalar, value.B + scalar, value.C + scalar, value.D + scalar);
		}

		public static SHL1 operator +(float scalar, SHL1 value)
		{
			return new SHL1(scalar + value.A, scalar + value.B, scalar + value.C, scalar + value.D);
		}

		public static SHL1 operator -(SHL1 value, float scalar)
		{
			return new SHL1(value.A - scalar, value.B - scalar, value.C - scalar, value.D - scalar);
		}

		public static SHL1 operator -(float scalar, SHL1 value)
		{
			return new SHL1(scalar - value.A, scalar - value.B, scalar - value.C, scalar - value.D);
		}

		public static bool operator ==(SHL1 left, SHL1 right)
		{
			return left.Equals(ref right);
		}

		public static bool operator !=(SHL1 left, SHL1 right)
		{
			return !left.Equals(ref right);
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.CurrentCulture, "L0:{0} L1:{1};{2};{3}", A, B, C, D);
		}

		private bool Equals(ref SHL1 other)
		{
			return (MathUtil.NearEqual(other.A, A) &&
					MathUtil.NearEqual(other.B, B) &&
					MathUtil.NearEqual(other.C, C) &&
					MathUtil.NearEqual(other.D, D));
		}

		public bool Equals(SHL1 other)
		{
			return Equals(ref other);
		}

		public override bool Equals(object value)
		{
			if (!(value is SHL1))
				return false;

			var strongValue = (SHL1)value;
			return Equals(ref strongValue);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = A.GetHashCode();
				hashCode = (hashCode * 397) ^ B.GetHashCode();
				hashCode = (hashCode * 397) ^ C.GetHashCode();
				hashCode = (hashCode * 397) ^ D.GetHashCode();
				return hashCode;
			}
		}

		#endregion
	}
}

