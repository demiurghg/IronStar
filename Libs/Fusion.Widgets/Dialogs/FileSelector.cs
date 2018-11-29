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

namespace Fusion.Widgets.Dialogs {

	static public class FileSelector {

		const int DialogWidth	= 560 + 4 + 4 + 4;
		const int DialogHeight	= 480 + 2 + 2 + 14 + 2 + 20 + 2;


		static public void ShowDialog ( FrameProcessor fp, string defaultDir, string searchPattern, string oldFileName, Action<string> setFileName )
		{
			var fileSelector	=	new FileSelectorFrame( fp, defaultDir, searchPattern, oldFileName, setFileName );

			fp.RootFrame.Add( fileSelector );
			fp.ModalFrame = fileSelector;

			fileSelector.CenterFrame();
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

			Label		labelDir;
			//ScrollBox	scrollBox;
			Button		buttonAccept;
			Button		buttonHome;
			Button		buttonPreview;
			Button		buttonClose;
			FileListBox	fileListBox;

			Frame		previewFrame;



			public FileSelectorFrame ( FrameProcessor fp, string defaultDir, string searchPattern, string oldFileName, Action<string> setFileName ) 
			: base ( fp, 0,0, DialogWidth, DialogHeight )
			{
				this.setFileName	=	setFileName;

				labelDir			=	new Label( fp, 2, 3, DialogWidth - 4, 10, "" );

				buttonAccept		=	new Button( fp, "Accept",	DialogWidth - 120 - 2, DialogHeight - 2 - 20, 120, 20, ()=>Accept() );
				buttonHome			=	new Button( fp, "Home",		DialogWidth - 240 - 4, DialogHeight - 2 - 20, 120, 20, ()=>Home() );
				buttonPreview		=	new Button( fp, "Preview",  DialogWidth - 360 - 6, DialogHeight - 2 - 20, 120, 20, ()=>Preview() );
				buttonClose			=	new Button( fp, "Close",  2,                     DialogHeight - 2 - 20, 120, 20, ()=>Close() );

				fileListBox			=	new FileListBox( fp, defaultDir, searchPattern );
				fileListBox.X		=	2;
				fileListBox.Y		=	14;
				fileListBox.Width	=	560+4+4;
				fileListBox.Height	=	480+4;

				fileListBox.IsDoubleClickEnabled = true;
				fileListBox.DoubleClick += FileListBox_DoubleClick;
				fileListBox.SelectedItemChanged+=FileListBox_SelectedItemChanged;

				labelDir.Text	=	fileListBox.CurrentDirectory;

				Add( buttonAccept );
				Add( buttonHome );
				Add( buttonPreview );
				Add( buttonClose );
				Add( fileListBox );
				Add( labelDir );

				Missclick += FileSelectorFrame_Missclick;
				Closed += FileSelectorFrame_Closed;
			}

			private void FileSelectorFrame_Closed( object sender, EventArgs e )
			{
				ClosePreview();
			}

			private void FileListBox_SelectedItemChanged( object sender, EventArgs e )
			{
				Preview();
			}

			
			private void FileListBox_DoubleClick( object sender, MouseEventArgs e )
			{
				Accept();
			}


			public void Home ()
			{
				fileListBox.ResetCurrentDirectory();
			}


			public void Preview ()
			{
				var selectedItem = fileListBox.SelectedItem;
				var content = Frames.Game.Content;

				ClosePreview();

				if (selectedItem==null || selectedItem.IsDirectory) {
					return;
				}

				var ext		= Path.GetExtension( selectedItem.RelativePath );
				var relName = selectedItem.RelativePath;
				var path	= ContentUtils.GetPathWithoutExtension( relName );
				var name	= Path.GetFileName(path);

				if (ext==".tga"	|| ext==".png" || ext==".jpg") {

					Log.Message("File selector preview : {0}", path );
					
					try {
						var image = content.Load<DiscTexture>(path);	

						var label = string.Format("{0}\r{1}x{2}", name, image.Width, image.Height );

						var w = image.Width;
						var h = image.Height;

						while (w>320 || h>320) {
							w /= 2;
							h /= 2;
						}

						while (Math.Max(w,h)<128) {
							w *= 2;
							h *= 2;
						}

						var fw = w + 4 + 2;
						var fh = h + 4 + 2 + 17;

						previewFrame = new Frame( Frames, 50,50, fw, fh, label, Color.Black );
						previewFrame.X				= this.X - fw - 5;
						previewFrame.Y				= this.Y;
						previewFrame.Padding		= 2;
						previewFrame.Border			= 1;
						previewFrame.BorderColor	= ColorTheme.BorderColorLight;
						previewFrame.BackColor		= ColorTheme.BackgroundColorDark;
						previewFrame.ForeColor		= ColorTheme.TextColorNormal;
						previewFrame.TextAlignment	= Alignment.BottomCenter;
						previewFrame.ShadowColor	= ColorTheme.ShadowColor;
						previewFrame.ShadowOffset	= new Vector2(1,1);
						previewFrame.Image			= image;
						previewFrame.ImageMode		= FrameImageMode.Manual;
						previewFrame.ImageDstRect	= new Rectangle(3,3,w,h);
						previewFrame.ImageSrcRect	= new Rectangle(0,0,image.Width,image.Height);
						previewFrame.ImageColor		= Color.White;

						Frames.RootFrame.Add( previewFrame );

					} catch ( Exception e ) {
						Log.Warning("{0}", e.Message);
					}
				}
			}


			public void Accept()
			{
				var selectedItem = fileListBox.SelectedItem;

				if (selectedItem==null) {
					return;
				}

				if (selectedItem.IsDirectory) {
					fileListBox.CurrentDirectory = selectedItem.FullPath;
				} else {
					setFileName( selectedItem.RelativePath );
					Close();
				}
			}


			void ClosePreview ()
			{
				if (previewFrame!=null) {
					Frames.RootFrame.Remove( previewFrame );
				}
			}


			private void FileSelectorFrame_Missclick( object sender, EventArgs e )
			{
				Close();
			}
		}
	}
}
