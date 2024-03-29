﻿using BEPUphysics.Paths;
using Fusion;
using Fusion.Core.Extensions;
using Fusion.Engine.Frames;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames.Layouts;
using Fusion.Widgets;
using Fusion.Widgets.Advanced;

namespace IronStar.Editor.Controls {
	public class AssetExplorer : Panel {

		readonly Type[] types;
		readonly FileListBox fileList;
		readonly JsonFactory factory;
		readonly Label labelPath;
		readonly Label labelName;
		readonly AEPropertyGrid grid;
		readonly ScrollBox scrollBox;
		readonly Label labelStatus;

		string targetFileName;
		object targetObject;
		
		public AssetExplorer( Frame parent, string initialDir, Type[] types, int x, int y, int w, int h ) : base(parent.Frames, x,y,600,500)
		{
			AllowDrag		=	true;

			Layout			=	new PageLayout( 12, 12, 2, 20, 3, 12, 1 );

			Padding			=	1;

			//------------------------

			this.types		=	types.Where( t => !t.IsAbstract ).ToArray();
			this.factory	=	parent.Game.GetService<JsonFactory>();

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


			labelPath = new Label( Frames, 2,2,600-4,10, fileList.CurrentDirectory );
			labelPath.TextAlignment = Alignment.MiddleLeft;
			labelPath.BackColor = ColorTheme.BackgroundColor;

			labelName = new Label( Frames, 2,2,600-4,10, "..." );
			labelName.TextAlignment = Alignment.MiddleLeft;

			labelStatus	= new Label( Frames, 0,0,0,0, "...");
			labelStatus.BackColor = ColorTheme.BackgroundColor;
			labelStatus.TextAlignment = Alignment.MiddleLeft;

			//------------------------

			this.Add(labelPath);
			this.Add(labelName);
			//this.Add( Frame.CreateEmptyFrame(Frames) );

			this.Add( fileList );
			this.Add( scrollBox );
			scrollBox.Add( grid );
	
			this.Add( new Button(Frames, "New Asset", 0,0,10,10, () => ShowNameDialog(parent, fileList) ) );
			this.Add( new Button(Frames, "Delete"	, 0,0,10,10, () => DeleteSelected() ) );
			this.Add( new Button(Frames, "Close"	, 0,0,10,10, () => this.Visible = false ) { RedButton = true } );

			this.Add( labelStatus );

			//------------------------

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



		int countdownTimer = 0;
		bool dirty1 = false;

		/// <summary>
		/// Saves target object each 500 msec if dirty.
		/// </summary>
		/// <param name="gameTime"></param>
		protected override void Update(GameTime gameTime)
		{
			base.Update( gameTime );

			int delta = MathUtil.Clamp((int)gameTime.Elapsed.TotalMilliseconds, 0, 500 );

			if (countdownTimer<=0) {
				countdownTimer += 500;
				if (dirty1) {
					SaveTargetObject();
				}
			}
			countdownTimer -= delta;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Grid_PropertyChanged(object sender, AEPropertyGrid.PropertyChangedEventArgs e)
		{
			dirty1 = true;
			labelStatus.Text = string.Format("Property changed: {0} {1}", e.Property.Name, e.Value.ToString() );
		}



		void SaveTargetObject ()
		{
			labelStatus.Text = "Changes saved...";
			//Log.Message("Saving object...");
			if (File.Exists(targetFileName)) {
				File.Delete(targetFileName);
			}
			using (var stream = File.OpenWrite(targetFileName)) {
				factory.ExportJson(stream, targetObject);
			}
			dirty1 = false;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FileList_SelectedItemChanged(object sender, EventArgs e)
		{
			try {
				labelPath.Text = fileList.CurrentDirectory;
				if (fileList.SelectedItem==null) {
					return;
				}
				if (!fileList.SelectedItem.IsDirectory) {
					targetFileName	=	fileList.SelectedItem.FullPath;

					using (var stream = File.OpenRead(targetFileName)) {
						targetObject = factory.ImportJson(stream);
					}

					labelName.Text	=	targetObject.GetType().Name + " - " + Path.GetFileNameWithoutExtension( targetFileName );

					grid.TargetObject = targetObject;
				}
			} catch ( Exception err ) {
				labelName.Text = "...";
				targetFileName	=	null;
				targetObject	=	null;
				grid.TargetObject = null;
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
					grid.TargetObject = null;
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
			var textBox	=	new TextBox( frames, null )	{ X = 2, Y = 200-22-11, Width=300-4, Height=10 };
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
						Game.GetService<JsonFactory>().ExportJson( stream, obj );
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
