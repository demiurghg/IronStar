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
	class VTStorage : DisposableBase, IStorage
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
				Log.Debug("close clusters:");

				foreach ( var pair in clusters )
				{
					Log.Message("Unloading VT cluster: {0:X8}...", pair.Key);
					pair.Value?.Dispose();
				}
				clusters.Clear();
			}
			base.Dispose( disposing );
		}

		public void CreateDirectory( string directoryName )	{ throw new NotSupportedException(); }
		public void DeleteDirectory( string directoryName )	{ throw new NotSupportedException(); }
		public bool DirectoryExists( string directoryName )	{ throw new NotSupportedException(); }


		ZipArchive Select( string fileName )
		{
			VTAddress address;

			if (!VTAddress.TryParse(fileName, out address))
			{
				throw new ArgumentException("Filename must be VT address");
			}

			ZipArchive clusterArchive;
			uint clusterId	=	address.GetClusterIndex();

			if (clusters.TryGetValue( clusterId, out clusterArchive ))
			{
				return clusterArchive;
			}
			else
			{
				if (clusterId==0x00040004)
				{
					Log.Warning("BL");
				}
				var clusterName	=	clusterId.ToString("X8") + ".zip";
				var clusterPath	=	Path.Combine( path, clusterName );

				Log.Message("Loading VT cluster: {0:X8}...", clusterId);

				clusterArchive	=	ZipFile.Open( clusterPath, mode );

				clusters.Add( clusterId, clusterArchive );

				return clusterArchive;
			}
		}

		public void DeleteFile( string fileName )
		{
			lock (lockObj)
			{
				Select(fileName).GetEntry(fileName)?.Delete();
			}
		}

		public bool FileExists( string fileName )
		{
			lock (lockObj)
			{
				return Select(fileName).GetEntry(fileName)!=null;
			}
		}

		public string[] GetFiles( string directory, string searchPattern, bool recursive )
		{
			throw new NotSupportedException();
		}

		public string GetFullPath( string fileName )
		{
			throw new NotSupportedException();
		}

		public DateTime GetLastWriteTimeUtc( string fileName )
		{
			lock (lockObj)
			{
				var entry = Select(fileName).GetEntry(fileName);
				if (entry!=null)
				{
					return entry.LastWriteTime.DateTime;
				}
				else
				{
					throw new FileNotFoundException(string.Format("File {0} not found", fileName));
				}
			}
		}

		public Stream OpenFile( string fileName, FileMode fileMode, FileAccess fileAccess )
		{
			lock (lockObj)
			{
				switch (fileMode)
				{
					case FileMode.CreateNew:	throw new NotSupportedException();
					case FileMode.OpenOrCreate:	throw new NotSupportedException();
					case FileMode.Truncate:		throw new NotSupportedException();
					case FileMode.Append:		throw new NotSupportedException();
					case FileMode.Create:		return OpenWrite(fileName);
					case FileMode.Open:			return OpenRead(fileName);
				}

				Trace.Assert(false);
				return null;
			}
		}

		public Stream OpenRead( string fileName )
		{
			lock (lockObj)
			{
				if (!FileExists(fileName))
				{
					throw new FileNotFoundException(string.Format("File {0} not found", fileName));
				}
				else
				{
					return Select(fileName).GetEntry(fileName).Open();
				}
			}
		}

		public Stream OpenWrite( string fileName )
		{
			lock (lockObj)
			{
				var archive =	Select(fileName);
					archive.GetEntry(fileName)?.Delete();
				
				var entry	=	archive.CreateEntry(fileName, CompressionLevel.Fastest);
					entry.LastWriteTime	=	DateTime.Now;
				return entry.Open();
			}
		}
	}
}
