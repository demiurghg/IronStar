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
		public static readonly GlobalPatchIndex Empty = new GlobalPatchIndex(0,0,0);

		public GlobalPatchIndex( int x, int y, int mip )
		{
			if ( x    < 0 || x    >= 4096 ) throw new ArgumentOutOfRangeException("x"	, x		, "0 <= x < 4096");
			if ( y    < 0 || y    >= 4096 ) throw new ArgumentOutOfRangeException("y"	, y		, "0 <= y < 4096");
			if ( mip  < 0 || mip  >= 8    ) throw new ArgumentOutOfRangeException("mip"	, mip	, "0 <= mip < 8");

			uint ux		= (uint)(x)		& 0xFFF;
			uint uy		= (uint)(y)		& 0xFFF;
			uint umip	= (uint)(mip)	& 0x7;

			Index = (ux << 20) | (uy << 8) | (umip << 5);
		}


		public GlobalPatchIndex( uint index )
		{
			Index	=	index;
		}


		public GlobalPatchIndex( Int2 xy, int mip ) : this( xy.X, xy.Y, mip )
		{
		}


		public GlobalPatchIndex( Int3 xyz ) : this( xyz.X, xyz.Y, xyz.Z )
		{
		}


		public readonly uint Index;
		

		public int X
		{
			get { return (int)( ( Index >> 20 ) & 0xFFF ); }
		}


		public int Y
		{
			get { return (int)( ( Index >> 8 ) & 0xFFF ); }
		}


		public int Mip
		{
			get { return (int)( ( Index >> 5 ) & 0x7 ); }
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
