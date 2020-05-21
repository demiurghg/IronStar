﻿using System;
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
		public readonly int HitCount;

		public CachedPatchIndex( int cacheIndex, int direction, int hitCount )
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
				uint uDirection		=	(uint)(Direction  & 0xFFF);
				uint uHitCount		=	(uint)(HitCount	  & 0xFF);

				return ( uCacheIndex << 20 ) | ( uDirection << 8 ) | ( uHitCount << 0);
			}
		}
	}
}
