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
		public readonly GenericImage<Bool>		Coverage;

		public readonly GenericImage<SHL1>		TempSHs;


		/// <summary>
		/// Creates instance of the lightmap g-buffer :
		/// </summary>
		/// <param name="w"></param>
		/// <param name="h"></param>
		public LightMapGBuffer( int w, int h ) 
		{
			Width			=	w;
			Height			=	h;

			Albedo			=	new GenericImage<Color>		( w, h, Color.Zero	 );
			Position		=	new GenericImage<Vector3>	( w, h, Vector3.Zero );
			PositionOld		=	new GenericImage<Vector3>	( w, h, Vector3.Zero );
			Normal			=	new GenericImage<Vector3>	( w, h, Vector3.Zero );
			Coverage		=	new GenericImage<Bool>		( w, h, false );

			TempSHs			=	new GenericImage<SHL1>( w, h, SHL1.Zero );
		}
			




		/// <summary>
		/// Bilateral blur
		/// </summary>
		public void BlurRadianceBilateral ()
		{
			/*for ( int i=1; i<Width-1; i++ ) {
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

						TempSHs[i,j] = c / c.Alpha;
					}
				}
			}//*/

			//TempSHs.CopyTo( Radiance );		  */
		}
	}

}
