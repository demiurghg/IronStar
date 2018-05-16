using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System.Reflection;
using Fusion.Core.Mathematics;
using Fusion;
using Fusion.Core.Input;
using System.IO;
using Fusion.Build;
using Fusion.Core.Content;

namespace IronStar.Editor2.Controls {

	static public class FileSelector {

		static FileSelectorFrame fileSelector;

		const int DialogWidth	= 560 + 4 + 4 + 4;
		const int DialogHeight	= 480 + 2 + 2 + 14 + 2 + 20 + 2;


		static public void ShowDialog ( FrameProcessor fp, string defaultDir, string searchPattern, string oldFileName, Action<string> setFileName )
		{
			var fileSelector	=	new FileSelectorFrame( fp, defaultDir, searchPattern, oldFileName, setFileName );

			fp.RootFrame.Add( fileSelector );
			fp.ModalFrame = fileSelector;

			FrameUtils.CenterFrame( fileSelector );
		}


		class ListItem {
			public readonly bool IsDirectory;
			public readonly string Name;
			public readonly string DisplayName;

			string SizeToString ( long size ) {
				if (size<1024) return size.ToString() + "   ";
				if (size<1024*1024) return (size/1024).ToString() + " Kb";
				return (size/1024/1024).ToString() + " Mb";
			}

			public ListItem ( string name, bool dir, string disp = null ) 
			{
				Name		=	name;
				IsDirectory	=	dir;

				DisplayName	=	disp ?? string.Format("{0,1}{1,-40}{2,-12}{3,12}",
					IsDirectory ? "\\" : "",
					IsDirectory ? Path.GetFileName(Name) : Path.GetFileNameWithoutExtension(Name),
					IsDirectory ? "" : Path.GetExtension( Name ),
					IsDirectory ? "Folder" : SizeToString( new FileInfo(Name).Length )
					);
			}
			
			public override string ToString()
			{
				return DisplayName;
			}
		}



		class FileSelectorFrame : Panel {

			readonly Action<string> setFileName;
			readonly string		oldFileName;
			readonly string		contentDir;
			readonly string		searchPattern;
					 string		currentDir;
			readonly string		homeDir;

			Label		labelDir;
			ScrollBox	scrollBox;
			Button		buttonAccept;
			Button		buttonHome;
			Button		buttonClose;
			ListBox		fileListBox;



			public FileSelectorFrame ( FrameProcessor fp, string defaultDir, string searchPattern, string oldFileName, Action<string> setFileName ) 
			: base ( fp, 0,0, DialogWidth, DialogHeight )
			{
				this.contentDir		=	Builder.FullInputDirectory;

				this.oldFileName	=	oldFileName;
				this.setFileName	=	setFileName;
				this.searchPattern	=	searchPattern;
				this.currentDir		=	Path.Combine( contentDir, defaultDir );
				this.homeDir		=	Path.Combine( contentDir, defaultDir );

				labelDir		=	new Label( fp, 2, 3, DialogWidth - 4, 10, Path.Combine( contentDir, defaultDir ) );

				scrollBox				=	new ScrollBox( fp, 2, 14, 560+4+4, 480+4 );
				scrollBox.Border		=	1;
				scrollBox.BorderColor	=	ColorTheme.BorderColorLight;

				buttonAccept	=	new Button( fp, "Accept", DialogWidth - 140 - 2, DialogHeight - 2 - 20, 140, 20, ()=>Accept() );
				buttonHome		=	new Button( fp, "Home",   DialogWidth - 280 - 4, DialogHeight - 2 - 20, 140, 20, ()=>Home() );
				buttonClose		=	new Button( fp, "Close",  2,                     DialogHeight - 2 - 20, 140, 20, ()=>Close() );

				fileListBox		=	new ListBox( fp, new object[0] );
				fileListBox.IsDoubleClickEnabled = true;
				fileListBox.DoubleClick += FileListBox_DoubleClick;
				fileListBox.SelectedItemChanged+=FileListBox_SelectedItemChanged;

				Add( buttonAccept );
				Add( buttonHome );
				Add( buttonClose );
				Add( scrollBox );
				Add( labelDir );
				scrollBox.Add( fileListBox );

				RefreshFileList();

				Missclick += FileSelectorFrame_Missclick;
			}


			private void FileListBox_SelectedItemChanged( object sender, EventArgs e )
			{
				var selectedItem = fileListBox.SelectedItem as ListItem;
				
				/*if (selectedItem!=null) {
					buttonAccept.Text = selectedItem.IsDirectory ? "Open Folder" : "Select File";
				} */
			}

			
			private void FileListBox_DoubleClick( object sender, MouseEventArgs e )
			{
				Accept();
			}



			public void Close ()
			{
				Frames.RootFrame.Remove( this );
				Frames.ModalFrame = null;
			}


			public void Home ()
			{
				currentDir = homeDir;
				RefreshFileList();
			}


			public void Accept()
			{
				var selectedItem = fileListBox.SelectedItem as ListItem;

				if (selectedItem==null) {
					return;
				}

				if (selectedItem.IsDirectory) {
					currentDir = selectedItem.Name;
					RefreshFileList();
				} else {
					var relName = ContentUtils.MakeRelativePath( contentDir + "\\", selectedItem.Name );
					setFileName( relName );
					Close();
				}
			}



			void RefreshFileList (  )
			{
				labelDir.Text	=	currentDir;

				var itemList = new List<ListItem>();

				var dirs = Directory
					.EnumerateDirectories( currentDir, "*", SearchOption.TopDirectoryOnly )
					.Select( dir => new ListItem(dir,true) )
					.ToList();

				var files = Directory
					.EnumerateFiles( currentDir, searchPattern, SearchOption.TopDirectoryOnly )
					.Select( file => new ListItem(file,false) )
					.ToList();

				var parentDir = Directory.GetParent( currentDir )?.FullName;

				if (parentDir!=null) {
					itemList.Add( new ListItem(parentDir, true, "..") );
				}

				itemList.AddRange( dirs );
				itemList.AddRange( files );

				fileListBox.SetItems(itemList);
			}


			private void FileSelectorFrame_Missclick( object sender, EventArgs e )
			{
				Close();
			}
		}
	}
}
