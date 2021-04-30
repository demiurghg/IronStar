using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using Fusion.Build.Mapping;

namespace Fusion.Engine.Graphics 
{
	public class ShadowCascade 
	{
		public readonly int SizeInTexels;
		public readonly int Index;
		public readonly Color Color;

		public ShadowCascade ( int index, int sizeInTexels, Color color )
		{
			this.Index			=	index;
			this.SizeInTexels	=	sizeInTexels;
			this.Color			=	color;
		}


		public bool IsActive 
		{
			get { return true; }
		}


		public int DetailLevel 
		{
			get { return 0; }
		}


		public Matrix ViewMatrix;

		public Matrix ProjectionMatrix;


		public Matrix ViewProjectionMatrix {
			get {
				return ViewMatrix * ProjectionMatrix;
			}
		}


		public Matrix ComputeGradientMatrix () 
		{
			var matrix	=	ViewMatrix * ProjectionMatrix;
				matrix	=	Matrix.Invert	( matrix );
				matrix	=	Matrix.Transpose( matrix );

			var size	=	(float)SizeInTexels;

				matrix	=	matrix * Matrix.Scaling( -2.0f/size, 2.0f/size, 1 );

			return matrix;
		}


		public Rectangle ShadowRegion;


		public Vector4 ShadowScaleOffset;
	}

}
