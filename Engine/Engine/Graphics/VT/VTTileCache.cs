﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using Fusion.Core.Shell;
using System.Runtime.InteropServices;
using Fusion.Build.Mapping;

namespace Fusion.Engine.Graphics {

	[StructLayout(LayoutKind.Explicit, Size=12)]
	public struct PageGpu 
	{
		public PageGpu ( uint vx, uint vy, uint offsetX, uint offsetY, uint mip )
		{
			this.VX			= vx;
			this.VY			= vy;
			this.PAddr		= Encode( offsetX, offsetY, mip );
		}

		[FieldOffset( 0)] public uint VX;
		[FieldOffset( 4)] public uint VY;
		[FieldOffset( 8)] public uint PAddr;

		static uint Encode(uint px, uint py, uint mip)
		{
			//	[y/n][res][x:13][y:13][mip:4]
			return 
				0x80000000 |
				(( px  & 0x1FFF ) << 17 ) |
				(( py  & 0x1FFF ) <<  4 ) |
				(( mip & 0x000F ) <<  0 ) ;
		}
	}



	public class VTTileCache 
	{
		class Page 
		{
			public Page ( VTAddress va, int pa, int physPageCount, int physicalTexSize )
			{
				this.VA			=	va;
				this.Address	=	pa;

				var physTexSize	=	(float)physicalTexSize;
				var border		=	VTConfig.PageBorderWidth;
				var pageSize	=	VTConfig.PageSizeBordered;

				this.X			=	(uint)((pa % (physPageCount)) * pageSize + border );
				this.Y			=	(uint)((pa / (physPageCount)) * pageSize + border );
			}
			
			public readonly VTAddress VA;
			public readonly int Address;
			public readonly uint X;
			public readonly uint Y;

			public VTTile Tile = null;

			public override string ToString ()
			{
				return string.Format("{0} {1} {2}", Address, X, Y );
			}
		}


		readonly int pageCount;
		readonly int capacity;
		readonly int physTexSize;
		LRUCache<VTAddress,Page> cache;

		readonly object lockObj = new object();

		public int Capacity {
			get { return capacity; }
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="size">Physical page count</param>
		public VTTileCache ( int physPageCount, int physTexSize )
		{
			this.pageCount		=	physPageCount;
			this.capacity		=	physPageCount * physPageCount;
			this.physTexSize	=	physTexSize;

			Purge();
		}



		/// <summary>
		/// Clears cache
		/// </summary>
		public void Purge ()
		{
			lock (lockObj)
			{
				cache	=	new LRUCache<VTAddress,Page>( capacity );

				//	fill cache with dummy pages :
				for (int i=0; i<capacity; i++) 
				{
					var va		= VTAddress.CreateBadAddress(i);
					var page	= new Page( va, i, pageCount, physTexSize );
					cache.Add( va, page );
				}
			}
		}



		/// <summary>
		/// Translates virtual texture address to physical rectangle in physical texture.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="rectangle"></param>
		/// <returns>False if address is not presented in cache</returns>
		public bool TranslateAddress ( VTAddress address, VTTile tile, out Rectangle rectangle )
		{
			lock (lockObj)
			{
				Page page;

				if (cache.TryGetValue(address, out page)) 
				{
					var pa		=	page.Address;
					var ppc		=	pageCount;
					var size	=	VTConfig.PageSizeBordered;
					int x		=	(pa % ppc) * size;
					int y		=	(pa / ppc) * size;
					int w		=	size;
					int h		=	size;
					rectangle 	=	new Rectangle( x,y,w,h );

					page.Tile	=	tile;

					return true;
				}
				else 
				{
					rectangle	=	new Rectangle();
					return false;
				}
			}
		}



		/// <summary>
		/// Gets gpu data for compute shader that updates page table
		/// </summary>
		/// <returns></returns>
		public PageGpu[] GetGpuPageData ()
		{
			lock (lockObj)
			{
				return cache.GetValues()
					.Where( pair1 => pair1.Tile!=null )
					.Select( pair2 => new PageGpu( 
						(uint)pair2.VA.PageX, 
						(uint)pair2.VA.PageY, 
						pair2.X,
						pair2.Y,
						(uint)pair2.VA.MipLevel ) )
					.ToArray();
			}
		}



		/// <summary>
		/// Adds new page to cache.
		///		
		///	If page exists:
		///		- LFU index of existing page is shifted.
		///		- returns FALSE
		/// 
		/// If page with given address does not exist:
		///		- page added to cache.
		///		- some pages could be evicted
		///		- return TRUE
		///		
		/// </summary>
		/// <param name="address"></param>
		/// <returns>False if page is already exist</returns>
		public bool Add ( VTAddress virtualAddress, out int physicalAddress )
		{
			lock (lockObj)
			{
				Page page;

				if (cache.TryGetValue( virtualAddress, out page )) 
				{
					physicalAddress	=	page.Address;
					return false;
				} 
				else 
				{
					cache.Discard( out page );

					var newPage	=	new Page( virtualAddress, page.Address, pageCount, physTexSize );

					cache.Add( virtualAddress, newPage ); 

					physicalAddress	=	newPage.Address;

					return true;
				}
			}
		}
	}
}