using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Content;
using Fusion.Core.Configuration;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Core;
using Fusion.Engine.Graphics.Lights;
using Fusion.Core.Shell;

namespace Fusion.Engine.Graphics.GI
{
	public partial class Radiosity
	{
		public static int EncodeDirection( Vector3 dir )
		{
			dir.Normalize();
			var xy = new Vector2( dir.X, dir.Y );
				xy.Normalize();
				xy		*= (float)Math.Sqrt( -dir.Z * 0.5f + 0.5f );
				xy.X	=	xy.X * 0.5f + 0.5f;
				xy.Y	=	xy.Y * 0.5f + 0.5f;
			
			uint ux	=	(uint)(xy.X * 32) & 0x1F;
			uint uy	=	(uint)(xy.Y * 32) & 0x1F;

			return (int)(ux << 5 | uy);
		}


		public Vector3 DecodeDirection( int dir )
		{
			if (dir>63) Log.Warning("bad direction");

			uint	ux	=	(uint)(( dir >> 3 ) & 0x7);
			uint	uy	=	(uint)(( dir >> 0 ) & 0x7);

			float	fx	=	ux / 8.0f;
			float	fy	=	uy / 8.0f;

			Vector4	nn	=	new Vector4(fx,fy,0,0) * new Vector4(2,2,0,0) + new Vector4(-1,-1,1,-1);
			float	l	=	- ( nn.X * nn.X + nn.Y * nn.Y + nn.Z * nn.W );
			nn.Z		=	2 * l - 1;
			nn.X		*=	(float)Math.Sqrt( l ) * 2;
			nn.Y		*=	(float)Math.Sqrt( l ) * 2;

			return new Vector3( nn.X, nn.Y, nn.Z );
		}


		public static int GetDirectionLutIndex ( Vector3 dir )
		{
			dir.Normalize();

			int bestIndex	= -1;
			var error		=  9999.0f;

			for (int i=0; i<DirLut.Length; i++)
			{
				var dist = Vector3.Distance( DirLut[i], dir );

				if (dist < error)
				{
					bestIndex = i;
					error     = dist;
				}
			}

			return bestIndex;
		}

		public static readonly Vector3[] DirLut = new Vector3[] {
			new Vector3(  0.00000f,  1.00000f,  0.00000f ),
			new Vector3( -0.24804f,  0.96875f,  0.00000f ),
			new Vector3(  0.00000f,  0.93750f,  0.34799f ),
			new Vector3(  0.00000f,  0.90625f, -0.42274f ),
			new Vector3(  0.34233f,  0.87500f,  0.34233f ),
			new Vector3( -0.37953f,  0.84375f, -0.37953f ),
			new Vector3( -0.41222f,  0.81250f,  0.41222f ),
			new Vector3(  0.44139f,  0.78125f, -0.44139f ),
			new Vector3(  0.61109f,  0.75000f,  0.25312f ),
			new Vector3( -0.64234f,  0.71875f, -0.26607f ),
			new Vector3( -0.27790f,  0.68750f,  0.67091f ),
			new Vector3(  0.28875f,  0.65625f, -0.69711f ),
			new Vector3(  0.29873f,  0.62500f,  0.72120f ),
			new Vector3( -0.30793f,  0.59375f, -0.74340f ),
			new Vector3( -0.76386f,  0.56250f,  0.31640f ),
			new Vector3(  0.78272f,  0.53125f, -0.32422f ),
			new Vector3(  0.84938f,  0.50000f,  0.16895f ),
			new Vector3( -0.86636f,  0.46875f, -0.17233f ),
			new Vector3( -0.17543f,  0.43750f,  0.88194f ),
			new Vector3(  0.17827f,  0.40625f, -0.89620f ),
			new Vector3(  0.51503f,  0.37500f,  0.77079f ),
			new Vector3( -0.52171f,  0.34375f, -0.78080f ),
			new Vector3( -0.78983f,  0.31250f,  0.52775f ),
			new Vector3(  0.79791f,  0.28125f, -0.53314f ),
			new Vector3(  0.80507f,  0.25000f,  0.53793f ),
			new Vector3( -0.81133f,  0.21875f, -0.54211f ),
			new Vector3( -0.54572f,  0.18750f,  0.81672f ),
			new Vector3(  0.54875f,  0.15625f, -0.82126f ),
			new Vector3(  0.19356f,  0.12500f,  0.97309f ),
			new Vector3( -0.19423f,  0.09375f, -0.97647f ),
			new Vector3( -0.97887f,  0.06250f,  0.19471f ),
			new Vector3(  0.98031f,  0.03125f, -0.19500f ),
			new Vector3(  0.99518f,  0.00000f,  0.09802f ),
			new Vector3( -0.99470f, -0.03125f, -0.09797f ),
			new Vector3( -0.09783f, -0.06250f,  0.99324f ),
			new Vector3(  0.09759f, -0.09375f, -0.99080f ),
			new Vector3(  0.62942f, -0.12500f,  0.76695f ),
			new Vector3( -0.62660f, -0.15625f, -0.76352f ),
			new Vector3( -0.75930f, -0.18750f,  0.62314f ),
			new Vector3(  0.75429f, -0.21875f, -0.61903f ),
			new Vector3(  0.85392f, -0.25000f,  0.45643f ),
			new Vector3( -0.84632f, -0.28125f, -0.45237f ),
			new Vector3( -0.44779f, -0.31250f,  0.83775f ),
			new Vector3(  0.44267f, -0.34375f, -0.82818f ),
			new Vector3(  0.26910f, -0.37500f,  0.88711f ),
			new Vector3( -0.26525f, -0.40625f, -0.87442f ),
			new Vector3( -0.86050f, -0.43750f,  0.26103f ),
			new Vector3(  0.84530f, -0.46875f, -0.25642f ),
			new Vector3(  0.82873f, -0.50000f,  0.25139f ),
			new Vector3( -0.81073f, -0.53125f, -0.24593f ),
			new Vector3( -0.24001f, -0.56250f,  0.79120f ),
			new Vector3(  0.23358f, -0.59375f, -0.77000f ),
			new Vector3(  0.36798f, -0.62500f,  0.68845f ),
			new Vector3( -0.35569f, -0.65625f, -0.66545f ),
			new Vector3( -0.64044f, -0.68750f,  0.34232f ),
			new Vector3(  0.61317f, -0.71875f, -0.32775f ),
			new Vector3(  0.51130f, -0.75000f,  0.41961f ),
			new Vector3( -0.48253f, -0.78125f, -0.39600f ),
			new Vector3( -0.36983f, -0.81250f,  0.45064f ),
			new Vector3(  0.34050f, -0.84375f, -0.41490f ),
			new Vector3(  0.04745f, -0.87500f,  0.48179f ),
			new Vector3( -0.04144f, -0.90625f, -0.42071f ),
			new Vector3( -0.34631f, -0.93750f,  0.03411f ),
			new Vector3(  0.24684f, -0.96875f, -0.02431f ),
		};
	}
}
