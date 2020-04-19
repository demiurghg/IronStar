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
		public readonly GenericImage<float>		Area;
		public readonly GenericImage<bool>		Coverage;

		public readonly GenericImage<int>		SampleCount;
		public			GenericImage<byte>		SampleGrade;
		public			GenericImage<byte>		PatchSizes;
		public			GenericImage<int>		Contribution;

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
			Area			=	new GenericImage<float>		( size, size, 0 );
			DirectLight		=	new GenericImage<Color4>	( size, size, Color4.Zero );
			Coverage		=	new GenericImage<bool>		( size, size, false );

			SampleCount		=	new GenericImage<int>		( size, size, 0 );
			SampleGrade		=	new GenericImage<byte>		( size, size, 0 );
			PatchSizes		=	new GenericImage<byte>		( size, size, 0 );
			Contribution	=	new GenericImage<int>		( size, size, 0 );

			IrradianceR		=	new GenericImage<SHL1>( size, size );
			IrradianceG		=	new GenericImage<SHL1>( size, size );
			IrradianceB		=	new GenericImage<SHL1>( size, size );
			temporary		=	new GenericImage<SHL1>( size, size );
		}
			

		public bool IsRegionCollapsable( Rectangle rect )
		{
			for (int i=rect.Left; i<=rect.Right; i++)
			{
				for (int j=rect.Top; j<rect.Bottom; j++)
				{
					if (Albedo[i,j].A==0)
					{
						return false;
					}
				}
			}
			return true;
		}



		public void ComputePatchSizes()
		{
			for ( byte sz = 1; sz <=32; sz*=2 )
			{
				ComputePatchSize(sz);
			}
		}


		void ComputePatchSize(byte size)
		{
			int w = Albedo.Width;
			int h = Albedo.Height;

			for (int i=0; i<w; i+=size)
			{
				for (int j=0; j<w; j+=size)
				{
					var rect = new Rectangle(i,j,size,size);
					if (IsRegionCollapsable(rect))
					{
						PatchSizes.FillRect( rect, size );
					}
				}
			}
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
