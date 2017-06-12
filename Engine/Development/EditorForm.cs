using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Fusion;
using Fusion.Build;
using Fusion.Engine.Common;
using Fusion.Development;
using Fusion.Core.Extensions;
using Fusion.Build.Mapping;
using System.Reflection;
using Fusion.Core.Shell;
using Fusion.Core;

namespace Fusion.Development {
	public partial class EditorForm : Form {

		readonly Game game;

		ConfigEditorControl configEditor;
		ObjectEditorControl	modelEditor;
		ObjectEditorControl	entityEditor;
		ObjectEditorControl	itemEditor;
		ObjectEditorControl	fxEditor;
		//MapEditorControl	mapEditor;
		//VTEditor		vtEditor;
		ObjectEditorControl	vtEditor;


		/// <summary>
		/// 
		/// </summary>
		public EditorForm( Game game )
		{
			this.game		=	game;
			
			InitializeComponent();

			configEditor	=	new ConfigEditorControl( game ) { Dock = DockStyle.Fill };
			//modelEditor		=	new ObjectEditorControl( game, "models",	typeof(ModelDescriptor), "Model"  ) { Dock = DockStyle.Fill };
			//entityEditor	=	new ObjectEditorControl( game, "entities",	typeof(EntityFactory), "Entity" ) { Dock = DockStyle.Fill };
			//itemEditor		=	new ObjectEditorControl( game, "items",		typeof(ItemFactory), "Items" ) { Dock = DockStyle.Fill };
			//fxEditor		=	new ObjectEditorControl( game, "fx",		typeof(FXFactory), "FX" ) { Dock = DockStyle.Fill };
			//mapEditor		=	new MapEditorControl( game ) { Dock = DockStyle.Fill };
			//vtEditor		=	new ObjectEditorControl( game, "vt",		typeof(VTTextureContent), "Megatexture" ) { Dock = DockStyle.Fill };

			mainTabs.TabPages["tabConfig"].Controls.Add( configEditor );
			//mainTabs.TabPages["tabModels"].Controls.Add( modelEditor );
			//mainTabs.TabPages["tabEntities"].Controls.Add( entityEditor );
			//mainTabs.TabPages["tabItems"].Controls.Add( itemEditor );
			//mainTabs.TabPages["tabMap"].Controls.Add( mapEditor );
			//mainTabs.TabPages["tabFX"].Controls.Add( fxEditor );
			//mainTabs.TabPages["tabMegatexture"].Controls.Add( vtEditor );

			InitializeToolStripMenu();

			Log.Message("Editor initialized");
		}



		/*public MapEditorControl MapEditor {
			get {
				return mapEditor;
			}
		} */



		void InitializeToolStripMenu ()
		{
			var targets = game.Config
				.TargetObjects
				.OrderBy( t1 => t1.Key )
				.ToList();

			foreach ( var target in targets ) {

				var methods = target.Value.GetType()
					.GetMethods( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic )
					.Where( m1 => m1.HasAttribute<BrowsableAttribute>() )
					.Where( m2 => m2.GetAttribute<BrowsableAttribute>().Browsable )
					.OrderBy( m3 => m3.GetAttribute<DisplayOrder>()?.Order )
					.ToArray();

				if (methods.Any()) {

					var toolItem = new ToolStripMenuItem( target.Key );
					menuStrip1.Items.Add( toolItem );

					int group = 0;

					foreach ( var method in methods ) {
						var name = method.GetAttribute<DisplayNameAttribute>()?.DisplayName ?? method.Name;
						var order = method.GetAttribute<DisplayOrder>()?.Order;

						if (method.GetParameters().Length>0) {
							name += "...";
						}

						var item = new ToolStripMenuItem( name, null, (s,e) => InvokeMethod(target.Value, method) );

						if (group!=order) {
							toolItem.DropDownItems.Add( new ToolStripSeparator() );
							group = order.GetValueOrDefault();
						}

						toolItem.DropDownItems.Add( item );
					}
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		/// <param name="method"></param>
		void InvokeMethod ( object target, MethodInfo method )
		{
			var parameters = method.GetParameters();

			var args = new object[0];
			
			if (parameters.Any()) {
				args = ArgumentDialog.Show( this, method.Name, parameters );
				if (args==null) {
					return;
				}
			}

			game.Invoke( () => method.Invoke(target, args ) );
		}



		public static void Run ( Game game )
		{
			var editorForm =	Application.OpenForms.Cast<Form>().FirstOrDefault( form => form is EditorForm );

			if (editorForm==null) {
				editorForm = new EditorForm(game);
			}

			editorForm.Show();
			editorForm.Activate();
			editorForm.BringToFront();
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Event handlers :
		 * 
		-----------------------------------------------------------------------------------------*/

		private void buttonExit_Click( object sender, EventArgs e )
		{
			Close();
		}

		private void buttonSaveAndBuild_Click( object sender, EventArgs e )
		{
			Close();
		}
	}
}
