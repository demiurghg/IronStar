﻿#define USE_PRIORITY_QUEUE
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
	internal class VTTileLoader : DisposableBase 
	{
		readonly IStorage storage;
		readonly VTSystem vt;

		object lockObj = new object();

		#if USE_PRIORITY_QUEUE
		ConcurrentPriorityQueue<int,VTAddress>	requestQueue;
		#else
		ConcurrentQueue<VTAddress>	requestQueue;
		#endif
		
		ConcurrentQueue<VTTile>		loadedTiles;

		Thread	loaderThread;
		bool	stopLoader = false;
		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="baseDirectory"></param>
		public VTTileLoader ( VTSystem vt, IStorage storage )
		{
			this.storage		=	storage;
			this.vt				=	vt;

			#if USE_PRIORITY_QUEUE
				requestQueue	=	new ConcurrentPriorityQueue<int,VTAddress>();
			#else
				requestQueue	=	new ConcurrentQueue<VTAddress>();
			#endif

			loadedTiles			=	new ConcurrentQueue<VTTile>();

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



		/// <summary>
		/// Gets loaded tile or zero
		/// </summary>
		/// <returns></returns>
		public bool TryGetTile ( out VTTile image )
		{
			return loadedTiles.TryDequeue( out image );
		}



		protected override void Dispose( bool disposing )
		{
			if ( disposing ) 
			{
				lock (lockObj) 
				{
					stopLoader	=	true;
				}
			}
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



		/// <summary>
		/// Functionas running in separate thread
		/// </summary>
		void LoaderTask ()
		{
			while (!stopLoader) 
			{
				using ( new CVEvent( "VT Loader Task" ) ) 
				{
					VTAddress address = default(VTAddress);
					KeyValuePair<int,VTAddress> result;

					if (!requestQueue.TryDequeue(out result)) 
					{
						//Thread.Sleep(1);
						continue;
					} 
					else 
					{
						address = result.Value;
					}

					var fileName = address.GetFileNameWithoutExtension(".tile");

					//Log.Message("...vt tile load : {0}", fileName );
					try 
					{
						using ( new CVEvent( "Reading Tile" ) ) 
						{
							var tile = VTTilePool.Alloc(address);

							tile.Read( storage.OpenFile( fileName, FileMode.Open, FileAccess.Read ) );

							loadedTiles.Enqueue( tile );
						}
					} 
					catch 
					( OutOfMemoryException oome ) 
					{
						//var tile = new VTTile( address );
						//tile.Clear( Color.Magenta );

						//loadedTiles.Enqueue( tile );

						Log.Error("VTTileLoader : {0}", oome.Message );
						Thread.Sleep(500);
					} 
					catch ( IOException ioex ) 
					{
						//var tile = new VTTile( address );
						//tile.Clear( Color.Magenta );

						//loadedTiles.Enqueue( tile );

						Log.Error("VTTileLoader : {0}", ioex.Message );
						Thread.Sleep(50);
					}
				}
			}
		}

	}
}
