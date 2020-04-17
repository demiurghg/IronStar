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
using Fusion.Build.Mapping;

namespace Fusion.Engine.Graphics.Lights 
{

	public class LightMapStaging 
	{
		public readonly int Width;
		public readonly int Height;

		public readonly GenericImage<Color>		Albedo;
		public readonly GenericImage<Vector3>	Position;
		public readonly GenericImage<Vector3>	PositionOld;
		public readonly GenericImage<Vector3>	Normal;
		public readonly GenericImage<Color4>	DirectLight;
		public readonly GenericImage<Bool>		Coverage;

		public readonly GenericImage<SHL1>		IrradianceR;
		public readonly GenericImage<SHL1>		IrradianceG;
		public readonly GenericImage<SHL1>		IrradianceB;

		readonly GenericImage<SHL1>				temporary;

		readonly Allocator2D					allocator;
		
		/// <summary>
		/// Creates instance of the lightmap g-buffer :
		/// </summary>
		/// <param name="w"></param>
		/// <param name="h"></param>
		public LightMapStaging( int size ) 
		{
			Width			=	size;
			Height			=	size;

			allocator		=	new Allocator2D( size );

			Albedo			=	new GenericImage<Color>		( size, size, Color.Zero	 );
			Position		=	new GenericImage<Vector3>	( size, size, Vector3.Zero );
			PositionOld		=	new GenericImage<Vector3>	( size, size, Vector3.Zero );
			Normal			=	new GenericImage<Vector3>	( size, size, Vector3.Zero );
			Coverage		=	new GenericImage<Bool>		( size, size, false );
			DirectLight		=	new GenericImage<Color4>	( size, size, Color4.Zero );

			IrradianceR		=	new GenericImage<SHL1>( size, size );
			IrradianceG		=	new GenericImage<SHL1>( size, size );
			IrradianceB		=	new GenericImage<SHL1>( size, size );
			temporary		=	new GenericImage<SHL1>( size, size );
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

						temporary[i,j] = c / c.Alpha;
					}
				}
			}

			temporary.CopyTo( Radiance );*/
		}
	}

}
