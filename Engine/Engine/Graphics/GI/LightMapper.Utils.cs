﻿using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Graphics.Ubershaders;
using Native.Embree;
using System.Runtime.InteropServices;
using Fusion.Core;
using System.Diagnostics;
using Fusion.Engine.Imaging;
using Fusion.Core.Configuration;
using Fusion.Build.Mapping;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Fusion.Engine.Graphics.Scenes;
using System.IO;

namespace Fusion.Engine.Graphics.Lights {

	partial class LightMapper {

		/// <summary>
		/// 
		/// </summary>
		/// <param name="lmGroups"></param>
		/// <param name="action"></param>
		void ForEachLightMapPixel (	IEnumerable<LightMapGroup> lmGroups, Action<int,int> action, bool showLog = false )
		{
			int count		=	0;
			int total		=	lmGroups.Sum( lmg => lmg.Region.Width * lmg.Region.Height );
			int one20th		=	total / 10;
			int progress	=	0;

			foreach ( var group in lmGroups )
			{
				var r = group.Region;

				for ( int i=r.Left; i<r.Right; i++ ) 
				{
					for ( int j=r.Top; j<r.Bottom; j++ ) 
					{
						count++;

						if (count>one20th && showLog)
						{
							progress++;
							Log.Message("...{0}/{1}", progress, 10 );
							count = 0;
						}

						action(i,j);
					}
				}
			}
		}


		Color InterpolateColor ( Color c0, Color c1, Color c2, float s, float t )
		{
			float q = 1 - s - t;
			return (q * c0) + (s * c1) + (t * c2);
		}


		Vector3 InterpolatePosition ( Vector3 p0, Vector3 p1, Vector3 p2, float s, float t )
		{
			float q = 1 - s - t;
			return (q * p0) + (s * p1) + (t * p2);
		}


		Vector3 InterpolateNormal ( Vector3 n0, Vector3 n1, Vector3 n2, float s, float t )
		{
			float q = 1 - s - t;
			return Vector3.Normalize( (q * n0) + (s * n1) + (t * n2) );
		}

		Vector2 InterpolateTexCoord ( Vector2 t0, Vector2 t1, Vector2 t2, float s, float t )
		{
			float q = 1 - s - t;
			return (q * t0) + (s * t1) + (t * t2);
		}


		float ComputeTriangleArea ( Vector3 a, Vector3 b, Vector3 c )
		{
			return 0.5f * ( Vector3.Cross( b - a, c - a ) ).Length();
		}

		float ComputeTriangleArea ( Vector2 a, Vector2 b, Vector2 c )
		{
			return Math.Abs( 0.5f * (
				a.X*(b.Y-c.Y) + b.X*(c.Y-a.Y) + c.X*(a.Y-b.Y)
			) );
		}


		float ComputeLightMapTexelArea( Vector3 p1, Vector3 p2, Vector3 p3, Vector2 t1, Vector2 t2, Vector2 t3 )
		{
			//	note, tex coords are in pixel coordinates
			float worldSpaceArea	=	ComputeTriangleArea( p1, p2, p3 );
			float lightMapArea		=	ComputeTriangleArea( t1, t2, t3 );

			return worldSpaceArea / lightMapArea;

			//int lmMinX			=	(int)(MathUtil.Min3( t1.X, t2.X, t3.X ) - 0.5f );
			//int lmMinY			=	(int)(MathUtil.Min3( t1.Y, t2.Y, t3.Y ) - 0.5f );

			//int lmMaxX			=	(int)(MathUtil.Max3( t1.X, t2.X, t3.X ) + 0.5f);
			//int lmMaxY			=	(int)(MathUtil.Max3( t1.Y, t2.Y, t3.Y ) + 0.5f);

			//int lmWidth			=	Math.Abs( lmMaxX - lmMinX );
			//int lmHeight		=	Math.Abs( lmMaxX - lmMinX );

			//int texelCount		=	lmWidth * lmHeight;
		}

	}
}