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
using Fusion.Engine.Frames.Layouts;

namespace Fusion.Widgets.Dialogs 
{
	static public class FileSelector 
	{
		const int DialogWidth	= 560 + 4 + 4 + 4;
		const int DialogHeight	= 480 + 2 + 2 + 14 + 2 + 20 + 2;

		enum Mode
		{
			Open,
			Save,
		}


		static public void ShowDialog ( FrameProcessor fp, string defaultDir, string searchPattern, string oldFileName, Action<string> commitAction )
		{
			var fileSelector	=	new FileSelectorFrame( fp, Mode.Open, defaultDir, searchPattern, oldFileName, commitAction );

			fp.ShowDialogCentered( fileSelector );
		}


		static public void ShowSaveDialog ( FrameProcessor fp, string defaultDir, string searchPattern, string oldFileName, Action<string> commitAction )
		{
			var fileSelector	=	new FileSelectorFrame( fp, Mode.Save, defaultDir, searchPattern, oldFileName, commitAction );

			fp.ShowDialogCentered( fileSelector );
		}


		class ListItem 
		{
			public readonly bool IsDirectory;
			public readonly string Name;
			public readonly string DisplayName;

			string SizeToString ( long size ) 
			{
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



		class FileSelectorFrame : Panel 
		{
			readonly Action<string> commitAction;
			readonly Mode mode;

			Label		labelDir;
			//ScrollBox	scrollBox;
			Button		buttonAccept;
			Button		buttonHome;
			Button		buttonClose;
			FileListBox	fileListBox;
			TextBox		fileTextBox;

			Frame		previewFrame;



			public FileSelectorFrame ( FrameProcessor fp, Mode mode, string defaultDir, string searchPattern, string oldFileName, Action<string> setFileName ) 
			: base ( fp, 0,0, DialogWidth, DialogHeight )
			{
				this.commitAction	=	setFileName;
				this.mode			=	mode;

				AllowDrag			=	true;
				AllowResize			=	true;

				Layout	=	new PageLayout()
						.AddRow(  17, new[] { -1f } )
						.AddRow( -1f, new[] { -1f } )
						.AddRow(  18, new[] { -1f } )
						.AddRow(  25, new[] { 80, -1f, 80, 80 } )
						.AddRow(  17, new[] { -1f } )
						.Margin(2)
						;

				labelDir			=	new Label( fp, 2, 3, DialogWidth - 4, 15, "" );

				fileTextBox			=	new TextBox( fp );
				fileTextBox.CommitEditsOnDeactivation = true;

				buttonAccept		=	new Button( fp, mode.ToString(),	0,0,0,0, ()=>Accept() );
				buttonHome			=	new Button( fp, "Home",				0,0,0,0, ()=>Home() );
				buttonClose			=	new Button( fp, "Cancel",			0,0,0,0, ()=>Close() );

				fileListBox			=	new FileListBox( fp, defaultDir, searchPattern );

				fileListBox.IsDoubleClickEnabled = true;
				fileListBox.DoubleClick += FileListBox_DoubleClick;
				fileListBox.SelectedItemChanged+=FileListBox_SelectedItemChanged;

				labelDir.Text	=	fileListBox.CurrentDirectory;

				Add( labelDir );
				Add( fileListBox );

				Add( fileTextBox );

				Add( buttonHome );
				Add( CreateEmptyFrame(fp) );
				Add( buttonAccept );
				Add( buttonClose );

				Missclick += FileSelectorFrame_Missclick;
				Closed += FileSelectorFrame_Closed;
			}

			
			private void FileSelectorFrame_Closed( object sender, EventArgs e )
			{
			}

			
			private void FileListBox_SelectedItemChanged( object sender, EventArgs e )
			{
				var selectedItem = fileListBox.SelectedItem;

				if (selectedItem!=null && !selectedItem.IsDirectory) 
				{
					fileTextBox.Text = selectedItem.FileName;
				}
			}

			
			private void FileListBox_DoubleClick( object sender, MouseEventArgs e )
			{
				Accept();
			}


			public void Home ()
			{
				fileListBox.ResetCurrentDirectory();
			}


			public void Accept()
			{
				var selectedItem = fileListBox.SelectedItem;

				if (selectedItem==null) 
				{
					return;
				}

				if (selectedItem.IsDirectory) 
				{
					fileListBox.CurrentDirectory = selectedItem.FullPath;
				}
				else 
				{
					string relativePath	= selectedItem.RelativePath;
					string fullPath		= selectedItem.FullPath;

					if (!string.IsNullOrWhiteSpace(fileTextBox.Text) && fileTextBox.Text!=selectedItem.FileName)
					{
						relativePath	= Path.Combine( fileListBox.CurrentDirectoryRelativePath, fileTextBox.Text );
						fullPath		= Path.Combine( fileListBox.CurrentDirectory			, fileTextBox.Text );
					}

					var fileExists = File.Exists( fullPath );

					if (mode==Mode.Open && !fileExists)
					{
						MessageBox.ShowError(Frames, 
							string.Format("File {0} does not exist", relativePath), 
							null 
						);
						return;
					}

					if (mode==Mode.Save && fileExists)
					{
						MessageBox.ShowQuestion(Frames, 
							string.Format("File {0} already exists. Overwrite?", relativePath), 
							() => { 
								commitAction(relativePath); 
								Close(); 
							}, 
							null 
						);
						return;
					}

					commitAction( relativePath );
					Close();
				}
			}





			private void FileSelectorFrame_Missclick( object sender, EventArgs e )
			{
				Close();
			}
		}
	}
}
