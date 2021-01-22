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
using Fusion.Core.Mathematics;

namespace Fusion.Core.Extensions
{
    /// <summary>
    /// Random functions on common types.
    /// </summary>
    public static class RandomExtensions
    {
		public static T SelectRandom<T>( this Random random, params T[] options )
		{
			if (options.Length==0) return default(T);

			return options[ random.Next(options.Length) ];
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="random"></param>
		/// <param name="interiorRadius"></param>
		/// <param name="exteriorRadius"></param>
		/// <returns></returns>
		public static Vector3 UniformRadialDistribution ( this Random random, float interiorRadius, float exteriorRadius )
		{
			Vector3 r;
			do {
				r	=	random.NextVector3( -Vector3.One, Vector3.One );
			} while ( r.Length() < 0.1f || r.Length() > 1 );

			r.Normalize();

			r = r * random.NextFloat( interiorRadius, exteriorRadius );

			return r;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="random"></param>
		/// <param name="interiorRadius"></param>
		/// <param name="exteriorRadius"></param>
		/// <returns></returns>
		public static Vector3 GaussRadialDistribution ( this Random random, float meanRadius, float stdDev )
		{
			Vector3 r;
			do {
				r	=	random.NextVector3( -Vector3.One, Vector3.One );
			} while ( r.Length() < 0.1f || r.Length() > 1 );

			r.Normalize();

			r = r * random.GaussDistribution( meanRadius, stdDev );

			return r;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="random"></param>
		/// <param name="innerRadius"></param>
		/// <param name="outerRadius"></param>
		/// <param name="tubeHeight"></param>
		/// <returns></returns>
		public static Vector3 UniformTubeDistribution ( this Random random, float innerRadius, float outerRadius, float tubeHeight )
		{
			float angle		=	random.NextFloat( 0, MathUtil.Pi * 2 );
			float radius	=	random.NextFloat( innerRadius, outerRadius );
			float height	=	random.NextFloat( -tubeHeight/2, tubeHeight/2 );
			float cos		=	(float)Math.Cos( angle );
			float sin		=	(float)Math.Sin( angle );
			return new Vector3( cos * radius, sin * radius, height );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="random"></param>
		/// <param name="mean"></param>
		/// <param name="stdDev"></param>
		/// <returns></returns>
		public static float GaussDistribution ( this Random random, float mean, float stdDev )
		{
			//Random rand = new Random(); //reuse this if you are generating many
			double u1 = random.NextDouble(); //these are uniform(0,1) random doubles
			double u2 = random.NextDouble();
			double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
						 Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
			double randNormal =
						 mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)

			return (float)randNormal;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="random"></param>
		/// <param name="mean"></param>
		/// <param name="stdDev"></param>
		/// <returns></returns>
		public static Vector3 UniformBoxDistribution ( this Random random, float w, float h, float d )
		{
			float x = random.NextFloat( -w/2, w/2 );
			float y = random.NextFloat( -h/2, h/2 );
			float z = random.NextFloat( -d/2, d/2 );
			return new Vector3(x,y,z);
		}



		/// <summary>
        /// Gets random <see cref="Vector3"/> on sphere.
		/// </summary>
		/// <param name="random">Current <see cref="System.Random"/>.</param>
		/// <param name="radius">Sphere radius</param>
		/// <returns></returns>
		public static Vector3 NextVector3OnSphere ( this Random random )
		{
			Vector3 r;
			do {
				r = NextVector3( random, -Vector3.One, Vector3.One );
			} while ( r.Length()>1 || r.Length()<0.1f );
			return r.Normalized();
		}


        /// <summary>
        /// Gets random <c>float</c> number within range.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <c>float</c> number.</returns>
        public static float NextFloat(this Random random, float min, float max)
        {
            return MathUtil.Lerp(min, max, (float)random.NextDouble());
        }

        /// <summary>
        /// Gets random <c>double</c> number within range.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <c>double</c> number.</returns>
        public static double NextDouble(this Random random, double min, double max)
        {
            return MathUtil.Lerp(min, max, random.NextDouble());
        }

        /// <summary>
        /// Gets random <c>long</c> number.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <returns>Random <c>long</c> number.</returns>
        public static long NextLong(this Random random)
        {
            var buffer = new byte[sizeof(long)];
            random.NextBytes(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }

        /// <summary>
        /// Gets random <c>long</c> number within range.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <c>long</c> number.</returns>
        public static long NextLong(this Random random, long min, long max)
        {
            byte[] buf = new byte[sizeof(long)];
            random.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return (Math.Abs(longRand % (max - min + 1)) + min);
        }

        /// <summary>
        /// Gets random <see cref="Vector2"/> within range.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <see cref="Vector2"/>.</returns>
        public static Vector2 NextVector2(this Random random, Vector2 min, Vector2 max)
        {
            return new Vector2(random.NextFloat(min.X, max.X), random.NextFloat(min.Y, max.Y));
        }

        /// <summary>
        /// Gets random <see cref="Vector3"/> within range.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <see cref="Vector3"/>.</returns>
        public static Vector3 NextVector3(this Random random, Vector3 min, Vector3 max)
        {
            return new Vector3(random.NextFloat(min.X, max.X), random.NextFloat(min.Y, max.Y), random.NextFloat(min.Z, max.Z));
        }

        /// <summary>
        /// Gets random <see cref="Vector4"/> within range.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <see cref="Vector4"/>.</returns>
        public static Vector4 NextVector4(this Random random, Vector4 min, Vector4 max)
        {
            return new Vector4(random.NextFloat(min.X, max.X), random.NextFloat(min.Y, max.Y), random.NextFloat(min.Z, max.Z), random.NextFloat(min.W, max.W));
        }

        /// <summary>
        /// Gets random opaque <see cref="Color"/>.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <returns>Random <see cref="Color"/>.</returns>
        public static Color NextColor(this Random random)
        {
            return new Color(random.NextFloat(0.0f, 1.0f), random.NextFloat(0.0f, 1.0f), random.NextFloat(0.0f, 1.0f), 1.0f);
        }

        /// <summary>
        /// Gets random opaque <see cref="Color"/>.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <returns>Random <see cref="Color"/>.</returns>
        public static Color4 NextColor4(this Random random)
        {
            return new Color4(random.NextFloat(0.0f, 1.0f), random.NextFloat(0.0f, 1.0f), random.NextFloat(0.0f, 1.0f), 1.0f);
        }

        /// <summary>
        /// Gets random opaque <see cref="Color"/>.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="minBrightness">Minimum brightness.</param>
        /// <param name="maxBrightness">Maximum brightness</param>
        /// <returns>Random <see cref="Color"/>.</returns>
        public static Color NextColor(this Random random, float minBrightness, float maxBrightness)
        {
            return new Color(random.NextFloat(minBrightness, maxBrightness), random.NextFloat(minBrightness, maxBrightness), random.NextFloat(minBrightness, maxBrightness), 1.0f);
        }

        /// <summary>
        /// Gets random <see cref="Color"/>.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>   
        /// <param name="minBrightness">Minimum brightness.</param>
        /// <param name="maxBrightness">Maximum brightness</param>
        /// <param name="alpha">Alpha value.</param>
        /// <returns>Random <see cref="Color"/>.</returns>
        public static Color NextColor(this Random random, float minBrightness, float maxBrightness, float alpha)
        {
            return new Color(random.NextFloat(minBrightness, maxBrightness), random.NextFloat(minBrightness, maxBrightness), random.NextFloat(minBrightness, maxBrightness), alpha);
        }

        /// <summary>
        /// Gets random <see cref="Color"/>.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="minBrightness">Minimum brightness.</param>
        /// <param name="maxBrightness">Maximum brightness</param>
        /// <param name="minAlpha">Minimum alpha.</param>
        /// <param name="maxAlpha">Maximum alpha.</param>
        /// <returns>Random <see cref="Color"/>.</returns>
        public static Color NextColor(this Random random, float minBrightness, float maxBrightness, float minAlpha, float maxAlpha)
        {
            return new Color(random.NextFloat(minBrightness, maxBrightness), random.NextFloat(minBrightness, maxBrightness), random.NextFloat(minBrightness, maxBrightness), random.NextFloat(minAlpha, maxAlpha));
        }

        /// <summary>
        /// Gets random <see cref="Point"/>.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <see cref="Point"/>.</returns>
        public static Point NextPoint(this Random random, Point min, Point max)
        {
            return new Point(random.Next(min.X, max.X), random.Next(min.Y, max.Y));
        }

        /// <summary>
        /// Gets random <see cref="System.TimeSpan"/>.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <see cref="System.TimeSpan"/>.</returns>
        public static TimeSpan NextTime(this Random random, TimeSpan min, TimeSpan max)
        {
            return TimeSpan.FromTicks(random.NextLong(min.Ticks, max.Ticks));
        }


		/// <summary>
		/// Gets uniformly distributes random point on given triangle
		/// </summary>
		/// <param name="rand"></param>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		/// <returns></returns>
		public static Vector3 NextPointOnTriangle ( this Random rand, Vector3 a, Vector3 b, Vector3 c )
		{
			var ab = b - a;
			var ac = c - a;
			
			float u,v;

			do {
				u	=	rand.NextFloat(0,1);	
				v	=	rand.NextFloat(0,1);
			} while ( u+v>1 );

			return a + ab * u + ac * v;
		}


		public static Vector3 NextUpHemispherePoint ( this Random random )
		{
			Vector3 r;
			do {
				r = NextVector3( random, -Vector3.One, Vector3.One );
			} while ( r.Length()>1 || r.Length()<0.1f || r.Y<0 );
			return r.Normalized();
		}
	}
}