#define USE_PRIORITY_QUEUE
using System;
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
using Fusion.Engine.Imaging;
using System.Threading;
using Fusion.Core.Collection;
using Fusion.Build.Mapping;

namespace Fusion.Engine.Graphics 
{
	internal class VTTileLoader
	{
		readonly VTSystem vt;

		object lockObj = new object();

		#if USE_PRIORITY_QUEUE
		ConcurrentPriorityQueue<int,VTAddress>	requestQueue;
		#else
		ConcurrentQueue<VTAddress>	requestQueue;
		#endif
		
		ConcurrentQueue<VTAddress[]>	feedbackQueue;
		ConcurrentQueue<VTTile>			loadedTiles;

		Thread	loaderThread;
		bool	stopLoader = false;

		VTTileCache	tileCache;
		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="baseDirectory"></param>
		public VTTileLoader ( VTSystem vt, VTTileCache tileCache )
		{
			this.vt				=	vt;
			this.tileCache		=	tileCache;

			#if USE_PRIORITY_QUEUE
				requestQueue	=	new ConcurrentPriorityQueue<int,VTAddress>();
			#else
				requestQueue	=	new ConcurrentQueue<VTAddress>();
			#endif

			loadedTiles			=	new ConcurrentQueue<VTTile>();
			feedbackQueue		=	new ConcurrentQueue<VTAddress[]>();

			loaderThread		=	new Thread( new ThreadStart( LoaderTask ) );
			loaderThread.Name	=	"VT Tile Loader Thread";
			loaderThread.IsBackground	=	true;
			loaderThread.Priority		=	ThreadPriority.BelowNormal;
			loaderThread.Start();
		}


		/// <summary>
		/// Request texture loading
		/// </summary>
		/// <param name="address"></param>
		public void RequestTile ( VTAddress address )
		{
			#if USE_PRIORITY_QUEUE
				requestQueue.Enqueue( 100-address.MipLevel, address );
			#else
				requestQueue.Enqueue( address );
			#endif
		}


		public void ReadFeedbackAndRequestTiles( VTAddress[] feedback )
		{
			if (feedback!=null)
			{
				feedbackQueue.Enqueue( feedback );
			}
		}


		/// <summary>
		/// Gets loaded tile or zero
		/// </summary>
		/// <returns></returns>
		public bool TryGetTile ( out VTTile image )
		{
			return loadedTiles.TryDequeue( out image );
		}



		public void StopAndWait()
		{
			stopLoader	=	true;
			loaderThread.Join();
		}


		public void Purge ()
		{
			lock (lockObj) 
			{
				requestQueue.Clear();

				VTTile tile;

				while (loadedTiles.TryDequeue(out tile)) 
				{
					VTTilePool.Recycle(tile);
				}
			}
		}


		List<VTAddress> BuildFeedbackVTAddressTree( VTAddress[] rawAddressData )
		{
			if (VTSystem.LockTiles) return new List<VTAddress>();

			var feedback = rawAddressData.Distinct().Where( p => !p.IsBad ).ToArray();

			List<VTAddress> feedbackTree = new List<VTAddress>();

			//	Build tree :
			foreach ( var addr in feedback ) 
			{
				var paddr = addr;

				if (addr.MipLevel<vt.LodBias) 
				{
					continue;
				}

				feedbackTree.Add( paddr );

				while (paddr.MipLevel < VTConfig.MaxMipLevel)
				{
					paddr = VTAddress.FromChild( paddr );
					feedbackTree.Add( paddr );
				}
			}

			//	Distinct :
			feedbackTree = feedbackTree
				.Distinct()
				//.Where( p0 => tileCache.Contains(p0) )
				.OrderByDescending( p1 => p1.MipLevel )
				.ToList();//*/

			//	Prevent thrashing:
			while (feedbackTree.Count >= tileCache.Capacity * 2 / 3 ) 
			{
				if (VTSystem.ShowThrashing) 
				{
					Log.Warning("VT thrashing: r:{0} a:{1}", feedbackTree.Count, tileCache.Capacity);
				}

				feedbackTree = feedbackTree.Select( a1 => a1.IsLeastDetailed ? a1 : a1.GetLessDetailedMip() )
					.Distinct()
					.OrderByDescending( p1 => p1.MipLevel )
					.ToList()
					;
			}

			return feedbackTree;
		}



		void UpdateCacheAndRequestTiles( List<VTAddress> feedback )
		{
			int counter = 0;

			//	Detect thrashing and prevention
			//	Get highest mip, remove them, repeat until no thrashing occur.
			/*while (feedbackTree.Count >= tileCache.Capacity * 2 / 3 ) 
			{
				if (ShowThrashing) Log.Warning("VT thrashing: r:{0} a:{1}", feedbackTree.Count, tileCache.Capacity);

				feedbackTree = feedbackTree.Select( a1 => a1.IsLeastDetailed ? a1 : a1.GetLessDetailedMip() )
					.Distinct()
					.OrderByDescending( p1 => p1.MipLevel )
					.ToList()
					;
			} */

			foreach ( var addr in feedback ) 
			{
				int physAddr;

				if ( tileCache.Add( addr, out physAddr ) ) 
				{
					RequestTile( addr );
					counter++;
				}

				if (counter>VTSystem.MaxPPF) 
				{
					break;
				}
			}
		}


		VTStorage LoadStorage()
		{
			return new VTStorage(@"Content\.vtstorage", true);
		}


		void LoaderTask ()
		{
			var storage		=	LoadStorage();

			while (!stopLoader) 
			{
				using ( new CVEvent( "VT Loader Task" ) ) 
				{
					VTAddress address = default(VTAddress);
					KeyValuePair<int,VTAddress> result;

					VTAddress[] feedbackBuffer;

					if (feedbackQueue.TryDequeue(out feedbackBuffer))
					{
						//	#PERf #MEMORY -- the following code is suspected in exccessive memory traffic
						var feedback = BuildFeedbackVTAddressTree(feedbackBuffer);
						UpdateCacheAndRequestTiles( feedback );
						vt.FeedbackBufferPool.Recycle( feedbackBuffer );
					}

					if (!requestQueue.TryDequeue(out result)) 
					{
						//Thread.Sleep(1);
						continue;
					} 
					else 
					{
						address = result.Value;
					}

					var fileName = address.GetFileNameWithoutExtension();

					try 
					{
						using ( new CVEvent( "Reading Tile" ) ) 
						{
							var tile = VTTilePool.Alloc(address);

							tile.Read( storage.OpenFile( fileName, FileMode.Open, FileAccess.Read ) );

							loadedTiles.Enqueue( tile );
						}
					} 
					catch ( OutOfMemoryException oome ) 
					{
						Log.Error("VTTileLoader : {0}", oome.Message );
						Thread.Sleep(500);
					} 
					catch ( IOException ioex ) 
					{
						Log.Error("VTTileLoader : {0}", ioex.Message );
						Thread.Sleep(50);
					}
				}
			}

			storage?.Dispose();
			storage = null;
		}

	}
}
