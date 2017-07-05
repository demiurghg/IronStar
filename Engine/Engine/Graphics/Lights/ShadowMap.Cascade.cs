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


namespace Fusion.Engine.Graphics {

	partial class ShadowMap {

		public class Cascade {

			readonly int sizeInTexels;

			public Cascade ( int sizeInTexels )
			{
				this.sizeInTexels	=	sizeInTexels;
			}


			public bool IsActive {
				get { return true; }
			}


			public int DetailLevel {
				get { return 0; }
			}


			public int CascadeSizeInTexels {
				get {
					return sizeInTexels;
				}
			}

			
			public float SlopeBias;
			

			public float DepthBias;
			

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

				var size	=	(float)CascadeSizeInTexels;

					matrix	=	matrix * Matrix.Scaling( -2.0f/size, 2.0f/size, 1 );

				return matrix;
			}


			public Rectangle ShadowRegion;


			public Vector4 ShadowScaleOffset;
		}

	}
}
