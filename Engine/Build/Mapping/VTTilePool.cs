using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Core.Mathematics;
using Fusion.Engine.Imaging;
using Fusion.Core.Utils;

namespace Fusion.Build.Mapping {
	
	public static class VTTilePool {

		const int TilePoolCapacity = 512;


		static FixedObjectPool<VTTile> tilePool;


		static VTTilePool ()
		{
			var data = Enumerable
				.Range( 0, TilePoolCapacity )
				.Select( i => new VTTile( VTAddress.CreateBadAddress(0) ) )
				.ToArray();

			tilePool	=	new FixedObjectPool<VTTile>( data );
		}


		static public VTTile Alloc ( VTAddress addr )
		{
			var tile = tilePool.Alloc();
			tile.VirtualAddress = addr;
			return tile;
		}


		static public void Recycle ( VTTile tile )
		{
			tile.VirtualAddress = VTAddress.CreateBadAddress(0);
			tilePool.Recycle( tile );
		}
	}
}
