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
using Fusion.Build;

namespace IronStar.Editor2.Controls {

	public class FileListBox : ListBox {

		public class FileListItem {
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

			public FileListItem( string fullPath, bool dir, string disp = null )
			{
				FullPath		=   fullPath;
				IsDirectory		=   dir;
				RelativePath	=	ContentUtils.MakeRelativePath( ContentDirectory + "\\", fullPath );

				DisplayName =   disp ?? string.Format( "{0,1}{1,-40}{2,-12}{3,12}",
					IsDirectory ? "\\" : "",
					IsDirectory ? Path.GetFileName( fullPath ) : Path.GetFileNameWithoutExtension( fullPath ),
					IsDirectory ? "" : Path.GetExtension( fullPath ),
					IsDirectory ? "Folder" : SizeToString( new FileInfo( fullPath ).Length )
					);
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
		public static string ContentDirectory {
			get {
				return Builder.FullInputDirectory;
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
				if (!Path.IsPathRooted(value)) {
					throw new ArgumentException("value must be rooted path");
				}
				if (!Directory.Exists(value)) {
					throw new ArgumentException("directory '" + value + "' does not exist");
				}
				currentDir = value;
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
		void RefreshFileList ()
		{
			var itemList = new List<FileListItem>();

			//	add top level directory (if exists) :
			var parentDir = Directory.GetParent( currentDir )?.FullName;

			if (parentDir!=null) {
				itemList.Add( new FileListItem(parentDir, true, "..") );
			}

			//	add sub-directories :
			var dirs = Directory
				.EnumerateDirectories( currentDir, "*", SearchOption.TopDirectoryOnly )
				.Select( dir => new FileListItem(dir,true) )
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

			itemList.AddRange( fileList.Select( file => new FileListItem(file,false) ) );

			SetItems(itemList);
		}

	}
}
