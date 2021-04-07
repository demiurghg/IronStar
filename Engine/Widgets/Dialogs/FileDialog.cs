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
using Fusion.Core.Extensions;
using Fusion.Core.Extensions;

namespace Fusion.Widgets.Dialogs 
{
	public abstract class FileDialog : Panel 
	{
		const int DialogWidth	= 560;
		const int DialogHeight	= 480;

		protected abstract string ButtonName { get; }

		Label		labelDir;
		Button		buttonAccept;
		Button		buttonHome;
		Button		buttonExplore;
		Button		buttonClose;
		FileListBox	fileListBox;
		TextBox		fileTextBox;


		public FileDialog ( FrameProcessor fp, string defaultDir, string searchPattern ) 
		: base ( fp, 0,0, DialogWidth, DialogHeight )
		{
			AllowDrag			=	true;
			AllowResize			=	true;

			Layout	=	new PageLayout()
					.AddRow(  20, new[] { -1f } )
					.AddRow( -1f, new[] { -1f } )
					.AddRow(  20, new[] { -1f } )
					.AddRow(  25, new[] { 80, 80, -1f, 80, 80 } )
					.AddRow(  17, new[] { -1f } )
					.Margin(2)
					;

			labelDir				=	new Label( fp, 2, 3, DialogWidth - 4, 15, "" );
			labelDir.Padding		=	2;
			labelDir.TextAlignment	=	Alignment.MiddleLeft;

			fileTextBox			=	new TextBox( fp );
			fileTextBox.CommitEditsOnDeactivation = true;
			fileTextBox.TextAlignment = Alignment.MiddleLeft;

			buttonAccept		=	new Button( fp, ButtonName,	0,0,0,0, ()=>AcceptInternal() );
			buttonHome			=	new Button( fp, "Home",		0,0,0,0, ()=>Home() );
			buttonExplore		=	new Button( fp, "Explore",	0,0,0,0, ()=>Explore() );
			buttonClose			=	new Button( fp, "Cancel",	0,0,0,0, ()=>Close() );

			fileListBox			=	new FileListBox( fp, defaultDir, searchPattern );

			fileListBox.IsDoubleClickEnabled = true;
			fileListBox.DoubleClick += FileListBox_DoubleClick;
			fileListBox.SelectedItemChanged+=FileListBox_SelectedItemChanged;

			labelDir.Text	=	fileListBox.CurrentDirectory;

			Add( labelDir );
			Add( fileListBox );

			Add( fileTextBox );

			Add( buttonHome );
			Add( buttonExplore );
			Add( CreateEmptyFrame(fp) );
			Add( buttonAccept );
			Add( buttonClose );

			Missclick += FileSelectorFrame_Missclick;
			Closed += FileSelectorFrame_Closed;
		}


		protected void ShowInternal()
		{ 
			Frames.ShowDialogCentered(this);
		}


		void FileSelectorFrame_Closed( object sender, EventArgs e )
		{
		}

			
		void FileListBox_SelectedItemChanged( object sender, EventArgs e )
		{
			var selectedItem = fileListBox.SelectedItem;

			if (selectedItem!=null && !selectedItem.IsDirectory) 
			{
				fileTextBox.Text = selectedItem.FileName;
			}

			if (selectedItem==null)
			{
				buttonAccept.Text = ButtonName;
			}
			else
			{
				buttonAccept.Text = selectedItem.IsDirectory ? "Open" : ButtonName;
			}
		}

			
		void FileListBox_DoubleClick( object sender, MouseEventArgs e )
		{
			AcceptInternal();
		}


		void Home ()
		{
			fileListBox.ResetCurrentDirectory();
			labelDir.Text = fileListBox.CurrentDirectory;
		}


		void Explore ()
		{
			Misc.ShellExecute(fileListBox.CurrentDirectory);
		}


		protected abstract bool Accept( string fullPath, string relativePath, bool fileExists );
		
		protected virtual string ApplyExtension( string fileName )
		{
			if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
			{
				return Path.ChangeExtension( fileName, fileListBox.DefaultExt );
			}

			return fileName;
		}




		void AcceptInternal()
		{
			var selectedItem = fileListBox.SelectedItem;

			if (selectedItem!=null && selectedItem.IsDirectory) 
			{
				fileListBox.CurrentDirectory = selectedItem.FullPath;
				labelDir.Text = fileListBox.CurrentDirectory;
				return;
			}

			var fileName = fileTextBox.Text;

			if (string.IsNullOrWhiteSpace(fileName))
			{
				MessageBox.ShowError(Frames, "Empty file name", null);
				return;
			}

			var relativePath	= Path.Combine( fileListBox.CurrentDirectoryRelativePath, fileName );
			var fullPath		= Path.Combine( fileListBox.CurrentDirectory			, fileName );

			if (Directory.Exists(fullPath))
			{
				fileListBox.CurrentDirectory = fullPath;
				return;
			}

			fullPath		=	ApplyExtension( fullPath );
			relativePath	=	ApplyExtension( relativePath );

			var fileExists = File.Exists(fullPath);

			if ( Accept( fullPath, relativePath, fileExists ) )
			{
				Close();
			}
		}


		void FileSelectorFrame_Missclick( object sender, EventArgs e )
		{
			Close();
		}
	}
}
