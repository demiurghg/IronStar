﻿using System;
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
using IronStar.SFX;
using Fusion.Development;
using Fusion.Core.Extensions;
using IronStar.Editors;
using IronStar.Core;
using Fusion.Build.Mapping;
using IronStar.Items;

namespace IronStar.Editors {
	public partial class EditorForm : Form {

		readonly Game game;

		ConfigEditorControl configEditor;
		ObjectEditorControl	modelEditor;
		ObjectEditorControl	entityEditor;
		ObjectEditorControl	weaponEditor;
		ObjectEditorControl	itemEditor;
		ObjectEditorControl	fxEditor;
		MapEditorControl	mapEditor;
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
			modelEditor		=	new ObjectEditorControl( game, "models",	typeof(ModelDescriptor), "Model"  ) { Dock = DockStyle.Fill };
			entityEditor	=	new ObjectEditorControl( game, "entities",	typeof(EntityFactory), "Entity" ) { Dock = DockStyle.Fill };
			weaponEditor	=	new ObjectEditorControl( game, "weapon",	typeof(WeaponFactory), "Weapon" ) { Dock = DockStyle.Fill };
			itemEditor		=	new ObjectEditorControl( game, "items",		typeof(ItemFactory), "Items" ) { Dock = DockStyle.Fill };
			fxEditor		=	new ObjectEditorControl( game, "fx",		typeof(FXFactory), "FX" ) { Dock = DockStyle.Fill };
			mapEditor		=	new MapEditorControl( game ) { Dock = DockStyle.Fill };
			vtEditor		=	new ObjectEditorControl( game, "vt",		typeof(VTTextureContent), "Megatexture" ) { Dock = DockStyle.Fill };

			mainTabs.TabPages["tabConfig"].Controls.Add( configEditor );
			mainTabs.TabPages["tabModels"].Controls.Add( modelEditor );
			mainTabs.TabPages["tabEntities"].Controls.Add( entityEditor );
			mainTabs.TabPages["tabWeapon"].Controls.Add( weaponEditor );
			mainTabs.TabPages["tabItems"].Controls.Add( itemEditor );
			mainTabs.TabPages["tabMap"].Controls.Add( mapEditor );
			mainTabs.TabPages["tabFX"].Controls.Add( fxEditor );
			mainTabs.TabPages["tabMegatexture"].Controls.Add( vtEditor );

			Log.Message("Editor initialized");
		}



		public MapEditorControl MapEditor {
			get {
				return mapEditor;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public void BuildContent ()
		{
			//mapEditor.SaveMap(false);
			//vtEditor.Save();

			Log.Message( "Building..." );
			Builder.SafeBuild();
			game.Reload();
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
			BuildContent();
		}

		private void exitToolStripMenuItem_Click( object sender, EventArgs e )
		{
			Close();
		}

		private void buildToolStripMenuItem_Click( object sender, EventArgs e )
		{
			BuildContent();
		}

		private void EditorForm_FormClosing( object sender, FormClosingEventArgs e )
		{
			//mapEditor.CloseMap();
			//vtEditor.Save();
		}
	}
}
