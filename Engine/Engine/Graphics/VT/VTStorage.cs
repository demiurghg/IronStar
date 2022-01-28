using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Build.Mapping;
using Fusion.Core;

namespace Fusion.Engine.Graphics 
{
	public class VTStorage : DisposableBase
	{
		readonly object lockObj = new object();
		readonly string path;
		readonly ZipArchiveMode mode;
		readonly Dictionary<uint,ZipArchive> clusters = new Dictionary<uint, ZipArchive>();


		public VTStorage( string path, bool read )
		{
			this.path	=	path;
			this.mode	=	read ? ZipArchiveMode.Read : ZipArchiveMode.Update;
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				foreach ( var pair in clusters )
				{
					Log.Message("Unloading VT cluster: {0:X8}...", pair.Key);
					pair.Value?.Dispose();
				}
				clusters.Clear();
			}
			base.Dispose( disposing );
		}


		ZipArchive LoadCluster( uint clusterId, ZipArchiveMode mode )
		{
			var clusterName	=	clusterId.ToString("X8") + ".zip";
			var clusterPath	=	Path.Combine( path, clusterName );
			Log.Message("Loading VT cluster: {0:X8} {1}...", clusterId, mode);

			if (mode==ZipArchiveMode.Read && !File.Exists(clusterPath))
			{
				return null;
			}
			else
			{
				return ZipFile.Open( clusterPath, mode );
			}
		}


		ZipArchive SelectRead( VTAddress address )
		{
			var	cluster		=	(ZipArchive)null;
			var clusterId	=	address.GetClusterIndex();

			if (clusters.TryGetValue( clusterId, out cluster ))
			{
				return cluster;
			} 
			else
			{
				cluster = LoadCluster(clusterId, ZipArchiveMode.Read);
				clusters.Add( clusterId, cluster );
				return cluster;
			}
		}


		ZipArchive SelectWrite( VTAddress address )
		{
			var	cluster		=	(ZipArchive)null;
			var clusterId	=	address.GetClusterIndex();

			if (clusters.TryGetValue( clusterId, out cluster ))
			{
				if (cluster==null || cluster.Mode==ZipArchiveMode.Read)
				{
					if (cluster!=null)
					{
						Log.Message("Unloading VT cluster: {0:X8}...", clusterId);
						cluster.Dispose();
					}

					cluster =  LoadCluster(clusterId, ZipArchiveMode.Update);
					clusters[clusterId] = cluster;
				}

				return cluster;
			} 
			else
			{
				cluster	=	LoadCluster(clusterId, ZipArchiveMode.Update);
				clusters.Add( clusterId, cluster );

				return cluster;
			}
		}

		
		public VTTile LoadTile( VTAddress address )
		{
			lock (lockObj)
			{
				var tile = new VTTile(address);

				if (TryLoadTile(address, tile))
				{
					return tile;
				}
				else
				{
					throw new FileNotFoundException(string.Format("File {0} not found", address));
				}
			}
		}


		public bool TryLoadTile( VTAddress address, VTTile dstTile )
		{
			lock (lockObj)
			{
				var entry = SelectRead(address)?.GetEntry( address.GetFileName() );

				if (entry!=null)
				{
					using ( var stream = entry.Open() )
					{
						dstTile.Read(stream);
						return true;
					}
				}
				else
				{
					return false;
				}
			}
		}


		public void SaveTile( VTAddress address, VTTile tile )
		{
			lock (lockObj)
			{
				var fileName	=	address.GetFileName();

				var archive		=	SelectWrite(address);
					archive.GetEntry(fileName)?.Delete();
				
				var entry		=	archive.CreateEntry(fileName, CompressionLevel.Fastest);

				using ( var stream = entry.Open() )
				{
					tile.Write( stream );
				}
			}
		}
	}
}
