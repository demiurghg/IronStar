using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.GI
{
	public struct PatchCachedIndex 
	{
		public readonly int CacheIndex;
		public readonly int Direction;
		public readonly int HitCount;

		public PatchCachedIndex( int cacheIndex, int direction, int hitCount )
		{
			CacheIndex	=	cacheIndex;
			Direction	=	direction;
			HitCount	=	hitCount;
		}


		public uint GpuIndex
		{
			get 
			{
				uint uCacheIndex	=	(uint)(CacheIndex & 0xFFF);
				uint uDirection		=	(uint)(CacheIndex & 0x3F);
				uint uHitCount		=	(uint)(CacheIndex & 0x3F);

				return ( uCacheIndex << 12 ) | ( uDirection << 6 ) | ( uHitCount << 0);
			}
		}
	}
}
