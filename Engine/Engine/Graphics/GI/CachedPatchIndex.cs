using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.GI
{
	public struct CachedPatchIndex 
	{
		public readonly int CacheIndex;
		public readonly int Direction;
		public readonly float Factor;

		public CachedPatchIndex( int cacheIndex, int direction, float factor )
		{
			CacheIndex	=	cacheIndex;
			Direction	=	direction;
			Factor		=	factor;
		}


		public uint GpuIndex
		{
			get 
			{
				uint uCacheIndex	=	(uint)(CacheIndex		) & 0xFFF;	//	12 bit
				uint uDirection		=	(uint)(Direction		) & 0x3FF;	//	10 bit
				uint uFactor		=	(uint)(Factor * 1023.0f	) & 0x3FF;	//	10 bit

				return ( uCacheIndex << 20 ) | ( uDirection << 10 ) | ( uFactor << 0);
			}
		}
	}
}
