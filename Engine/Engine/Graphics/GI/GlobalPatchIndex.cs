using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.GI
{
	public struct GlobalPatchIndex 
	{
		public int X;
		public int Y;
		public int Mip;
		public float Factor;


		public GlobalPatchIndex( int x, int y, int mip, float factor )
		{
			X		=	x;
			Y		=	y;
			Mip		=	mip;
			Factor	=	factor;
		}


		public uint Index 
		{
			get 
			{
				if ( X    < 0 || X    >= 4096 ) throw new ArgumentOutOfRangeException("x"	, X		, "0 <= x < 4096");
				if ( Y    < 0 || Y    >= 4096 ) throw new ArgumentOutOfRangeException("y"	, Y		, "0 <= y < 4096");
				if ( Mip  < 0 || Mip  >= 8    ) throw new ArgumentOutOfRangeException("mip"	, Mip	, "0 <= mip < 8");

				uint ux		= (uint)(X)		& 0xFFF;
				uint uy		= (uint)(Y)		& 0xFFF;
				uint umip	= (uint)(Mip)	& 0x7;

				return (ux << 20) | (uy << 8) | (umip << 5);
			}
		}


		public GlobalPatchIndex( Int2 xy, int mip, float factor ) : this( xy.X, xy.Y, mip, factor )
		{
		}


		public GlobalPatchIndex( Int3 xyz, float factor ) : this( xyz.X, xyz.Y, xyz.Z, factor )
		{
		}



		public Int3 Coords
		{
			get { return new Int3( X, Y, Mip ); }
		}


		public override string ToString()
		{
			uint 	lmMip		=	(Index >> 24) & 0xFF;
			uint 	lmX			=	(Index >> 12) & 0xFFF;
			uint 	lmY			=	(Index >>  0) & 0xFFF;
			return string.Format("{0,2} [{1,4} {2,4}]", Mip, X, Y );
		}
	}
}
