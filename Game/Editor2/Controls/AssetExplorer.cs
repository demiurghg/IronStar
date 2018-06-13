using BEPUphysics.Paths;
using Fusion;
using Fusion.Core.Extensions;
using Fusion.Engine.Frames;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Editor2.Controls {
	public class AssetExplorer : Panel {

		readonly Type[] types;
		readonly FileListBox fileList;
		readonly Factory factory;
		readonly Label label;
		readonly AEPropertyGrid grid;
		readonly ScrollBox scrollBox;

		string targetFileName;
		object targetObject;
		
		public AssetExplorer( Frame parent, string initialDir, Type[] types, int x, int y, int w, int h ) : base(parent.Frames, x,y,600,500)
		{
			this.types		=	types;
			this.factory	=	parent.Game.GetService<Factory>();

			fileList		=	new FileListBox( Frames, "", "*.json" );
			fileList.X		=	2;
			fileList.Y		=	14;
			fileList.Width	=	600/2 - 2;
			fileList.Height	=	500-14-2-22;
			fileList.DisplayMode	=	FileListBox.FileDisplayMode.ShortNoExt;

			scrollBox			=	new ScrollBox( Frames, 0,0,0,0 );
			scrollBox.X			=	600/2+1;
			scrollBox.Y			=	14;
			scrollBox.Width		=	600/2-3;
			scrollBox.Height	=	500-14-2-22;

			grid			=	new AEPropertyGrid( Frames );
			grid.X			=	600/2+1;
			grid.Y			=	14;
			grid.Width		=	600/2-3;
			grid.Height		=	500-14-2-22;

			this.Add( fileList );
			this.Add( scrollBox );

			scrollBox.Add( grid );

			label = new Label( Frames, 2,2,600-4,10, fileList.CurrentDirectory );
			label.TextAlignment = Alignment.MiddleLeft;
			this.Add(label);


			this.Add( new Button(Frames, "Close", 2, 500-22, 100, 20, () => this.Visible = false ) );
			this.Add( new Button(Frames, "New Asset", 600-102, 500-22, 100, 20, () => ShowNameDialog(parent, fileList) ) );
			this.Add( new Button(Frames, "Delete", 600-204, 500-22, 100, 20, () => DeleteSelected() ) );

			fileList.DoubleClick += (s,e) => {
				if (fileList.SelectedItem!=null && fileList.SelectedItem.IsDirectory) {
					fileList.CurrentDirectory = fileList.SelectedItem.FullPath;
				}
			};

			fileList.SelectedItemChanged += FileList_SelectedItemChanged;
			grid.PropertyChanged		 +=	Grid_PropertyChanged;

			parent.Add( this );
			this.CenterFrame();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Grid_PropertyChanged(object sender, AEPropertyGrid.PropertyChangedEventArgs e)
		{
			if (File.Exists(targetFileName)) {
				File.Delete(targetFileName);
			}
			using (var stream = File.OpenWrite(targetFileName)) {
				factory.ExportJson(stream, targetObject);
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FileList_SelectedItemChanged(object sender, EventArgs e)
		{
			try {
				label.Text = fileList.CurrentDirectory;
				if (fileList.SelectedItem==null) {
					return;
				}
				if (!fileList.SelectedItem.IsDirectory) {
					targetFileName	=	fileList.SelectedItem.FullPath;

					using (var stream = File.OpenRead(targetFileName)) {
						targetObject = factory.ImportJson(stream);
					}

					grid.FeedObjects( targetObject );
				}
			} catch ( Exception err ) {
				targetFileName	=	null;
				targetObject	=	null;
				grid.FeedObjects(null);
				Log.Warning(err.Message);
			}
		}

		
		/// <summary>
		/// Deletes selected file with warning.
		/// </summary>
		void DeleteSelected ()
		{
			var item = fileList.SelectedItem;

			if (item.IsDirectory) {
				MessageBox.ShowError(Parent, "Could not delete directory", null);
				return;
			}

			MessageBox.ShowQuestion(Parent, 
				string.Format("Delete file {0}?", item.RelativePath), 
				()=> {
					File.Delete(item.FullPath); 
					fileList.RefreshFileList();
				},
				null 
			);
		}

		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="fileListBox"></param>
		void ShowNameDialog ( Frame owner, FileListBox fileListBox )
		{
			var frames	=	owner.Frames;

			var panel	=	new Panel( frames, 0,0, 300, 200 );
			var listBox	=	new ListBox( frames, types )		{ X = 2, Y = 2, Width = 300-4, Height = 200-22-14 };
			var textBox	=	new TextBox( frames, null, null )	{ X = 2, Y = 200-22-11, Width=300-4, Height=10 };
				textBox.TextAlignment	=	Alignment.MiddleLeft;

			panel.Add( listBox );
			panel.Add( textBox );

			panel.Add( new Button(frames, "Cancel", 300- 80-2, 200-22, 80, 20, () => panel.Close() ) );
			panel.Add( new Button(frames, "OK",     300-160-4, 200-22, 80, 20, 
				() => {
					var type = listBox.SelectedItem as Type;
					if (type==null) {
						MessageBox.ShowError(owner, "Select asset type", null);
						return;
					}
					if (string.IsNullOrWhiteSpace(textBox.Text)) {
						MessageBox.ShowError(owner, "Provide asset name", null);
						return;
					}
					var obj  = Activator.CreateInstance(type);
					var path = Path.Combine( fileListBox.CurrentDirectory, textBox.Text + ".json" );

					using ( var stream = File.OpenWrite( path ) ) {
						Game.GetService<Factory>().ExportJson( stream, obj );
					}

					fileListBox.RefreshFileList();

					panel.Close();
				}
			));

			panel.Missclick += (s,e) => {
				panel.Close();
			};

			owner.Add( panel );
			panel.CenterFrame();
			frames.ModalFrame = panel;
		}

	}
}
