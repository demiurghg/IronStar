using System;
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

namespace Fusion.Engine.Graphics.Lights {


	public class LightMapGBuffer {

		public readonly int Width;
		public readonly int Height;

		public readonly GenericImage<Color>		Albedo;
		public readonly GenericImage<Vector3>	Position;
		public readonly GenericImage<Vector3>	PositionOld;
		public readonly GenericImage<Vector3>	Normal;
		public readonly GenericImage<Color4>	Radiance;
		public readonly GenericImage<Vector3>	Direction;
		public readonly GenericImage<Color4>	TempColor;
		public readonly GenericImage<Vector3>	TempDir;
		public readonly GenericImage<Bool>		Coverage;


		/// <summary>
		/// Creates instance of the lightmap g-buffer :
		/// </summary>
		/// <param name="w"></param>
		/// <param name="h"></param>
		public LightMapGBuffer( int w, int h ) 
		{
			Width		=	w;
			Height		=	h;

			Albedo		=	new GenericImage<Color>		( w, h, Color.Zero	 );
			Position	=	new GenericImage<Vector3>	( w, h, Vector3.Zero );
			PositionOld	=	new GenericImage<Vector3>	( w, h, Vector3.Zero );
			Normal		=	new GenericImage<Vector3>	( w, h, Vector3.Zero );
			Radiance	=	new GenericImage<Color4>	( w, h, Color4.Zero );
			Direction	=	new GenericImage<Vector3>	( w, h, Vector3.Zero );
			TempColor	=	new GenericImage<Color4>	( w, h, Color4.Zero );
			TempDir		=	new GenericImage<Vector3>	( w, h, Vector3.Zero );
			Coverage	=	new GenericImage<Bool>		( w, h, false );
		}
			


		/// <summary>
		/// Dilate lighting
		/// </summary>
		public void DilateRadiance ()
		{
			for ( int i=0; i<Width; i++ ) {
				for ( int j=0; j<Height; j++ ) {

					var c = Radiance[i,j];

					c	=	c.Alpha > 0 ? c : Radiance[i+1, j+0];
					c	=	c.Alpha > 0 ? c : Radiance[i-1, j+0];
					c	=	c.Alpha > 0 ? c : Radiance[i+0, j+1];
					c	=	c.Alpha > 0 ? c : Radiance[i+0, j-1];
					c	=	c.Alpha > 0 ? c : Radiance[i+1, j+1];
					c	=	c.Alpha > 0 ? c : Radiance[i-1, j-1];
					c	=	c.Alpha > 0 ? c : Radiance[i+1, j-1];
					c	=	c.Alpha > 0 ? c : Radiance[i-1, j+1];

					TempColor[i,j] = c;
				}
			}

			TempColor.CopyTo( Radiance );
		}



		/// <summary>
		/// Bilateral blur
		/// </summary>
		public void BlurRadianceBilateral ()
		{
			for ( int i=1; i<Width-1; i++ ) {
				for ( int j=1; j<Height-1; j++ ) {

					var c = Radiance[i+0, j+0];

					if (c.Alpha>0) {
						c	+=	Radiance[i+1, j+1];
						c	+=	Radiance[i+1, j+0];
						c	+=	Radiance[i+1, j-1];
						c	+=	Radiance[i+0, j+1];
						c	+=	Radiance[i+0, j-1];
						c	+=	Radiance[i-1, j+1];
						c	+=	Radiance[i-1, j+0];
						c	+=	Radiance[i-1, j-1];

						TempColor[i,j] = c / c.Alpha;
					}
				}
			}//*/

			TempColor.CopyTo( Radiance );
		}



		/// <summary>
		/// Blur direction
		/// </summary>
		public void BlurDirection ()
		{
			for ( int i=1; i<Width-1; i++ ) {
				for ( int j=1; j<Height-1; j++ ) {

					var d = Direction[i+0, j+0]  * 1.0f;

					d	+=	Direction[i+1, j+1] * 0.35f;
					d	+=	Direction[i+1, j-1] * 0.35f;
					d	+=	Direction[i-1, j-1] * 0.35f;
					d	+=	Direction[i-1, j+1] * 0.35f;

					d	+=	Direction[i+1, j+0] * 0.50f;
					d	+=	Direction[i+0, j+1] * 0.50f;
					d	+=	Direction[i+0, j-1] * 0.50f;
					d	+=	Direction[i-1, j+0] * 0.50f;

					TempDir[i,j] = d.Normalized();
				}
			}//*/

			TempDir.CopyTo( Direction );
		}


	}

}
