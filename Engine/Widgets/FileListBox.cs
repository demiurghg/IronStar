using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System.IO;
using Fusion.Core.Content;
using Fusion.Build;

namespace Fusion.Widgets {

	public class FileListBox : ListBox {

		public enum FileDisplayMode {
			ShortNoExt,
			Short,
			Full,
		}

		public class FileListItem {
			public readonly FileDisplayMode DisplayMode;
			public readonly bool IsDirectory;
			public readonly string FullPath;
			public readonly string DisplayName;
			public readonly string RelativePath;

			string SizeToString( long size )
			{
				if ( size<1024 ) return size.ToString() + "   ";
				if ( size<1024*1024 ) return ( size/1024 ).ToString() + " Kb";
				return ( size/1024/1024 ).ToString() + " Mb";
			}

			public FileListItem( FileListBox flb, FileDisplayMode displayMode, string fullPath, bool dir, string disp = null )
			{
				FullPath		=   fullPath;
				IsDirectory		=   dir;
				RelativePath	=	ContentUtils.MakeRelativePath( Path.GetFullPath(flb.ContentDirectory) + "\\", fullPath );

				switch (displayMode) {
					case FileDisplayMode.Full: 				
							DisplayName =   disp ?? string.Format( "{0,1}{1,-40}{2,-12}{3,12}",
							dir ? "\\" : "",
							dir ? Path.GetFileName( fullPath ) : Path.GetFileNameWithoutExtension( fullPath ),
							dir ? "" : Path.GetExtension( fullPath ),
							dir ? "Folder" : SizeToString( new FileInfo( fullPath ).Length )
							);
						break;
					case FileDisplayMode.Short: 				
							DisplayName =   disp ?? string.Format( "{0}{1}", dir ? "\\" : " ", Path.GetFileName( fullPath ) );
						break;
					case FileDisplayMode.ShortNoExt: 				
							DisplayName =   disp ?? string.Format( "{0}{1}", dir ? "\\" : " ", dir ? Path.GetFileName( fullPath ) : Path.GetFileNameWithoutExtension( fullPath ) );
						break;
				}
			}


			public override string ToString()
			{
				return DisplayName;
			}
		}


		readonly string[]	searchPatterns;
		readonly string		homeDir;
		private	 string		currentDir;


		/// <summary>
		/// Content directory
		/// </summary>
		public string ContentDirectory {
			get {
				return Game.Services.GetService<Builder>().GetBaseInputDirectory();
			}
		}


		/// <summary>
		/// Current directory
		/// </summary>
		public string CurrentDirectory {
			get {
				return currentDir;
			}
			set {
				var newDir = value;

				if (!Path.IsPathRooted(newDir)) {
					newDir = Path.Combine( ContentDirectory, newDir );
				}
				if (!Directory.Exists(newDir)) {
					throw new ArgumentException("directory '" + value + "' does not exist");
				}
				currentDir = newDir;
				RefreshFileList();
			}
		}



		FileDisplayMode fileDisplayMode = FileDisplayMode.Full;

		public FileDisplayMode DisplayMode {
			get {
				return fileDisplayMode;
			}
			set {
				fileDisplayMode = value;
				RefreshFileList();
			}
		}


		/// <summary>
		/// Selected file list item.
		/// </summary>
		public new FileListItem SelectedItem {
			get {
				return base.SelectedItem as FileListItem;
			}
		}


		/// <summary>
		/// Creates new instance of FileListBox
		/// </summary>
		/// <param name="fp"></param>
		public FileListBox ( FrameProcessor fp, string initialDirectory, string searchPattern ) : base(fp, new object[0])
		{
			this.searchPatterns	=	searchPattern.Split(' ',',',';','|').OrderBy(n=>n).ToArray();
			this.currentDir		=	Path.Combine( ContentDirectory, initialDirectory );
			this.homeDir		=	Path.Combine( ContentDirectory, initialDirectory );

			RefreshFileList();

			IsDoubleClickEnabled =	true;
		}



		/// <summary>
		/// Resets current directory
		/// </summary>
		public void ResetCurrentDirectory ()
		{
			CurrentDirectory = homeDir;
		}



		/// <summary>
		/// Updates file list for current directory.
		/// </summary>
		public void RefreshFileList ()
		{
			var itemList = new List<FileListItem>();

			//	add top level directory (if exists) :
			var parentDir = Directory.GetParent( currentDir )?.FullName;

			if (parentDir!=null) {
				itemList.Add( new FileListItem(this, fileDisplayMode, parentDir, true, "..") );
			}

			//	add sub-directories :
			var dirs = Directory
				.EnumerateDirectories( currentDir, "*", SearchOption.TopDirectoryOnly )
				.Select( dir => new FileListItem(this, fileDisplayMode, dir, true) )
				.ToList();

			itemList.AddRange( dirs );

			//	add files for each pattern :
			var fileList = new List<string>();
				
			foreach (var pattern in searchPatterns) {
				var files = Directory
					.EnumerateFiles( currentDir, pattern, SearchOption.TopDirectoryOnly )
					.ToList();

				fileList.AddRange( files );
			}

			itemList.AddRange( fileList.Select( file => new FileListItem(this, fileDisplayMode, file, false) ) );

			SetItems(itemList);
		}

	}
}
