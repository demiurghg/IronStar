// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.InteropServices;


namespace Fusion.Core.Mathematics
{
	[StructLayout(LayoutKind.Sequential)]
	public struct EulerAngles : IEquatable<EulerAngles>
	{
		public static readonly EulerAngles Zero = new EulerAngles(0, 0, 0);

		public static readonly EulerAngles Identity = Zero;

		public AngleSingle Yaw;

		public AngleSingle Pitch;

		public AngleSingle Roll;


		public EulerAngles(float yaw, float pitch, float roll, AngleType angleType = AngleType.Radian)
		{
			Yaw		=	new AngleSingle( yaw	, angleType );
			Pitch	=	new AngleSingle( pitch	, angleType );
			Roll	=	new AngleSingle( roll	, angleType );
		}


		public bool Equals(EulerAngles other)
		{
			return other.Yaw == Yaw && other.Pitch == Pitch && other.Roll == Roll;
		}


		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (obj.GetType() != typeof(Size3)) return false;
			return Equals((Size3)obj);
		}

		
		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Yaw.GetHashCode();
				hashCode = (hashCode * 397) ^ Pitch.GetHashCode();
				hashCode = (hashCode * 397) ^ Roll.GetHashCode();
				return hashCode;
			}
		}

		
		public static bool operator ==(EulerAngles left, EulerAngles right)
		{
			return left.Equals(right);
		}

		
		public static bool operator !=(EulerAngles left, EulerAngles right)
		{
			return !left.Equals(right);
		}

		
		public override string ToString()
		{
			return string.Format("({0},{1},{2})", Yaw, Pitch, Roll);
		}


		public Matrix ToMatrix()
		{
			return Matrix.RotationYawPitchRoll( Yaw.Radians, Pitch.Radians, Roll.Radians );
		}


		public Quaternion ToQuaternion()
		{
			return Quaternion.RotationYawPitchRoll( Yaw.Radians, Pitch.Radians, Roll.Radians );
		}


		public static EulerAngles RotationMatrix( Matrix matrix )
		{
			float yaw, pitch, roll;
			MathUtil.ToAngles( matrix, out yaw, out pitch, out roll );

			var angles	=	new EulerAngles( yaw, pitch, roll, AngleType.Radian );

			return angles;
		}


		public static EulerAngles RotationQuaternion( Quaternion quaternion )
		{
			return FromMatrix( Matrix.RotationQuaternion(quaternion) );
		}
	}
}