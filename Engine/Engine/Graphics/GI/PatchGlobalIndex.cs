using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.GI
{
	public struct PatchGlobalIndex 
	{
		public static readonly PatchGlobalIndex Empty = new PatchGlobalIndex(0,0,0,0);

		public PatchGlobalIndex( int x, int y, int mip, int hits )
		{
			if ( x    < 0 || x    >= 4096 ) throw new ArgumentOutOfRangeException("x"	, x		, "0 <= x < 4096");
			if ( y    < 0 || y    >= 4096 ) throw new ArgumentOutOfRangeException("y"	, y		, "0 <= y < 4096");
			if ( mip  < 0 || mip  >= 8    ) throw new ArgumentOutOfRangeException("mip"	, mip	, "0 <= mip < 8");
			if ( hits < 0 || hits >= 32   ) throw new ArgumentOutOfRangeException("hits", hits	, "0 <= hits < 32");

			uint ux		= (uint)(x)		& 0xFFF;
			uint uy		= (uint)(y)		& 0xFFF;
			uint umip	= (uint)(mip)	& 0x7;
			uint uhits	= (uint)(hits)	& 0x1F;

			Index = (ux << 20) | (uy << 8) | (umip << 5) | (uhits);
		}


		public PatchGlobalIndex( uint index )
		{
			Index	=	index;
		}


		public PatchGlobalIndex( Int2 xy, int mip, int hits ) : this( xy.X, xy.Y, mip, hits )
		{
		}


		public PatchGlobalIndex( Int3 xyz, int hits ) : this( xyz.X, xyz.Y, xyz.Z, hits )
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


		public int Hits
		{
			get { return (int)( ( Index >> 0 ) & 0x1F ); }
		}


		public Int3 Coords
		{
			get { return new Int3( X, Y, Mip ); }
		}


		public override string ToString()
		{
			if (Hits==0) 
			{	
				return string.Format("--------");
			}

			uint 	lmMip		=	(Index >> 24) & 0xFF;
			uint 	lmX			=	(Index >> 12) & 0xFFF;
			uint 	lmY			=	(Index >>  0) & 0xFFF;
			return string.Format("{0,2} [{1,4} {2,4}] {3}", Mip, X, Y, Hits );
		}
	}
}
