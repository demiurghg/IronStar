﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Fusion;
using Fusion.Build;
using Fusion.Engine.Common;
using IronStar.SFX;
using Fusion.Development;
using Fusion.Core.Extensions;

namespace IronStar.Editors {
	public partial class ObjectEditorControl : UserControl {
		
	
		class NamePathTarget {

			public string Name { get; set; }
			public string Path { get; set; } 

			readonly Type baseType;
			readonly Type[] extraTypes;

			public NamePathTarget ( string path, Type baseType, Type[] extraTypes ) 
			{
				this.baseType	=	baseType;
				this.extraTypes	=	extraTypes;
				Name = System.IO.Path.GetFileNameWithoutExtension(path);
				Path = path;
			}


			object target;
			public object Target {
				get {
					if (target==null) {
						LoadTarget();
					}
					return target;
				}
			}


			private object LoadTarget ()
			{
				target = Misc.LoadObjectFromXml( baseType, File.ReadAllText(Path), extraTypes ); 
				return target;
			}


			public void SaveTarget ()
			{
				File.WriteAllText( Path, Misc.SaveObjectToXml( target, baseType, extraTypes ) );
			}
		}



		readonly string fullSourceFolder;
		readonly Game game;
		readonly Type baseType;
		readonly Type[] extraTypes;
		readonly string objectName;




		/// <summary>
		/// 
		/// </summary>
		public ObjectEditorControl( Game game, string sourceFolder, Type baseTargetType, string objectName )
		{
			this.baseType	=	baseTargetType;
			this.game		=	game;
			this.objectName	=	objectName;
			
			this.extraTypes	=	Misc.GetAllSubclassesOf( baseTargetType, true )
								.Where( t1 => !t1.IsAbstract )
								.ToArray();
			
			InitializeComponent();
			this.objectPropertyGrid.LineColor = SystemColors.InactiveBorder;

			nameToolStripMenuItem.Text = objectName;


			PopulateActions(baseTargetType);


			fullSourceFolder = Path.Combine( Builder.FullInputDirectory, sourceFolder );

			if (!Directory.Exists(fullSourceFolder)) {
				Log.Warning("Directory {0} does not exist.", fullSourceFolder);
				Directory.CreateDirectory(fullSourceFolder);
			}

			RefreshFileList();

			objectPropertyGrid.PropertySort = PropertySort.Categorized;

			Log.Message("Object editor initialized");
		}



		void PopulateActions (Type type)
		{
			MethodInfo[] mia = type.GetMethods(BindingFlags.Public | BindingFlags.Instance );

			actionsToolStripMenuItem.Enabled = false;

			foreach (MethodInfo mi in mia) {

				if (!mi.HasAttribute<BrowsableAttribute>()) {
					continue;
				}

				if (!mi.GetAttribute<BrowsableAttribute>().Browsable) {
					continue;
				}

				var item    =   new ToolStripMenuItem();

				item.Text   =   mi.GetAttribute<DisplayNameAttribute>()?.DisplayName ?? mi.Name;

				item.Click  +=  ( s, e ) => {
					foreach ( var obj in objectListBox.SelectedItems.Cast<NamePathTarget>() ) {
						mi.Invoke( obj.Target, new object[0] );
						obj.SaveTarget();
					}
					this.objectPropertyGrid.Refresh();
				};

				actionsToolStripMenuItem.Enabled = true;
				actionsToolStripMenuItem.DropDownItems.Add( item );
			}


			actionsToolStripMenuItem.DropDownItems.Add( new ToolStripSeparator() );

			//-----------------------------

			mia = type.GetMethods( BindingFlags.Public | BindingFlags.Static );

			foreach (MethodInfo mi in mia) {

				if (!mi.HasAttribute<BrowsableAttribute>()) {
					continue;
				}

				if (!mi.GetAttribute<BrowsableAttribute>().Browsable) {
					continue;
				}

				var item    =   new ToolStripMenuItem();

				item.Text   =   mi.GetAttribute<DisplayNameAttribute>()?.DisplayName ?? mi.Name;

				item.Click  +=  ( s, e ) => {
					mi.Invoke(null, new object[0]);
					RefreshFileList();
					this.objectPropertyGrid.Refresh();
				};

				actionsToolStripMenuItem.Enabled = true;
				actionsToolStripMenuItem.DropDownItems.Add( item );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="action"></param>
		//public void AddEditAction ( string name, Action<object[]> action )
		//{
		//	editToolStripMenuItem.Enabled = true;

		//	var item    =   new ToolStripMenuItem();
		//	item.Text   =   name;
		//	item.Click  +=  ( s, e ) => {		
		//		var list = objectListBox.SelectedItems.Cast<object>().ToArray();
		//		action?.Invoke(list);
		//	};

		//	editToolStripMenuItem.DropDownItems.Add( item );
		//}



		/// <summary>
		/// 
		/// </summary>
		public void RefreshFileList ()
		{
			objectListBox.DisplayMember = "Name";
			objectListBox.ValueMember = "Path";
			
			var fileList = Directory
				.EnumerateFiles( fullSourceFolder, "*.xml" )
				.Select( fn => new NamePathTarget(fn, baseType, extraTypes) )
				.ToList();
			
			objectListBox.DataSource	=	fileList;
		}





		/// <summary>
		/// Runs create new model procedure.
		/// </summary>
		public void AddNewObjectUI ()
		{
			string name = "";

			while (true) {

				Type type = baseType;

				if (extraTypes.Length>1) {
					if (ObjectSelector.Show( this, "Select class", "Add", extraTypes.ToDictionary( tt => tt.Name ), out type )) {
					} else {
						return;
					}
				}

				name = NameDialog.Show( this, "Create new " + objectName + ":", "Add " + objectName, name);

				if (name==null) {
					return;
				}

				var fullPath = Path.Combine( fullSourceFolder, name + ".xml" );

				if (File.Exists( fullPath ) ) {
					var r = MessageBox.Show( this, string.Format("{0} '{1}' already exists", objectName, name), "Add", MessageBoxButtons.OKCancel );
					if ( r==DialogResult.OK ) {
						continue;
					} else {
						return;
					}
				}


				var newObject = Activator.CreateInstance( type );
				File.WriteAllText( fullPath, Misc.SaveObjectToXml( newObject, baseType, extraTypes ) );

				RefreshFileList();

				return;
			}
		}



		/// <summary>
		/// Runs object remove procedure.
		/// </summary>
		public void RemoveObjectUI ()
		{
			var r = MessageBox.Show(this, "Are you sure to remove selected items?", "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Warning );

			if (r!=DialogResult.Yes) {
				return;
			}

			var selected = objectListBox.SelectedItems.Cast<NamePathTarget>().ToArray();

			foreach ( var so in selected ) {
				File.Delete( so.Path );
			}

			RefreshFileList();
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Event handlers :
		 * 
		-----------------------------------------------------------------------------------------*/

		private void listBox1_SelectedIndexChanged( object sender, EventArgs e )
		{
			objectPropertyGrid.SelectedObjects = objectListBox
					.SelectedItems
					.Cast<NamePathTarget>()
					.Select( namePathTarget => namePathTarget.Target )
					.ToArray();

			
		}


		private void mainPropertyGrid_PropertyValueChanged( object s, PropertyValueChangedEventArgs e )
		{
			foreach ( var target in objectListBox.SelectedItems.Cast<NamePathTarget>() ) {
				target.SaveTarget();
			}
		}

		private void newToolStripMenuItem_Click( object sender, EventArgs e )
		{
			AddNewObjectUI();
		}

		private void deleteToolStripMenuItem_Click( object sender, EventArgs e )
		{
			RemoveObjectUI();
		}
	}
}
