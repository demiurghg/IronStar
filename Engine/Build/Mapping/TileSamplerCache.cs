using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Engine.Imaging;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Core;

namespace Fusion.Build.Mapping {

	class TileSamplerCache {

		LRUCache<VTAddress, VTTile> cache;

		readonly VTStorage storage;
			
		public TileSamplerCache ( VTStorage mapStorage )
		{
			this.storage	=	mapStorage;
			this.cache		=	new LRUCache<VTAddress,VTTile>(128);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public VTTile LoadImage ( VTAddress address )
		{
			VTTile tile;

			if (!cache.TryGetValue(address, out tile)) 
			{
				var path	=	address.GetFileName();
					tile	=	new VTTile(address);

				if (!storage.TryLoadTile(address, tile)) 
				{
					tile.Clear( Color.Black );
				}

				cache.Add( address, tile );
			}
			
			return tile;
		}
	}
}
