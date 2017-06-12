namespace Fusion.Development {
	partial class EditorForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing && ( components != null ) ) {
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.panel1 = new System.Windows.Forms.Panel();
			this.buttonExit = new System.Windows.Forms.Button();
			this.buttonBuild = new System.Windows.Forms.Button();
			this.mainTabs = new System.Windows.Forms.TabControl();
			this.tabConfig = new System.Windows.Forms.TabPage();
			this.tabModels = new System.Windows.Forms.TabPage();
			this.tabEntities = new System.Windows.Forms.TabPage();
			this.tabItems = new System.Windows.Forms.TabPage();
			this.tabFX = new System.Windows.Forms.TabPage();
			this.tabDecals = new System.Windows.Forms.TabPage();
			this.tabMegatexture = new System.Windows.Forms.TabPage();
			this.tabMap = new System.Windows.Forms.TabPage();
			this.panel1.SuspendLayout();
			this.mainTabs.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(570, 24);
			this.menuStrip1.TabIndex = 3;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.buttonExit);
			this.panel1.Controls.Add(this.buttonBuild);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel1.Location = new System.Drawing.Point(0, 526);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(570, 44);
			this.panel1.TabIndex = 4;
			// 
			// buttonExit
			// 
			this.buttonExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonExit.Location = new System.Drawing.Point(3, 11);
			this.buttonExit.Name = "buttonExit";
			this.buttonExit.Size = new System.Drawing.Size(80, 30);
			this.buttonExit.TabIndex = 2;
			this.buttonExit.Text = "Exit";
			this.buttonExit.UseVisualStyleBackColor = true;
			this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
			// 
			// buttonBuild
			// 
			this.buttonBuild.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonBuild.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.buttonBuild.Location = new System.Drawing.Point(466, 11);
			this.buttonBuild.Name = "buttonBuild";
			this.buttonBuild.Size = new System.Drawing.Size(100, 30);
			this.buttonBuild.TabIndex = 3;
			this.buttonBuild.Text = "Build";
			this.buttonBuild.UseVisualStyleBackColor = true;
			this.buttonBuild.Click += new System.EventHandler(this.buttonSaveAndBuild_Click);
			// 
			// mainTabs
			// 
			this.mainTabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mainTabs.Controls.Add(this.tabConfig);
			this.mainTabs.Controls.Add(this.tabModels);
			this.mainTabs.Controls.Add(this.tabEntities);
			this.mainTabs.Controls.Add(this.tabItems);
			this.mainTabs.Controls.Add(this.tabFX);
			this.mainTabs.Controls.Add(this.tabDecals);
			this.mainTabs.Controls.Add(this.tabMegatexture);
			this.mainTabs.Controls.Add(this.tabMap);
			this.mainTabs.HotTrack = true;
			this.mainTabs.Location = new System.Drawing.Point(3, 27);
			this.mainTabs.Name = "mainTabs";
			this.mainTabs.SelectedIndex = 0;
			this.mainTabs.Size = new System.Drawing.Size(564, 493);
			this.mainTabs.TabIndex = 5;
			// 
			// tabConfig
			// 
			this.tabConfig.Location = new System.Drawing.Point(4, 22);
			this.tabConfig.Name = "tabConfig";
			this.tabConfig.Padding = new System.Windows.Forms.Padding(3);
			this.tabConfig.Size = new System.Drawing.Size(556, 467);
			this.tabConfig.TabIndex = 5;
			this.tabConfig.Text = "Config";
			this.tabConfig.UseVisualStyleBackColor = true;
			// 
			// tabModels
			// 
			this.tabModels.Location = new System.Drawing.Point(4, 22);
			this.tabModels.Name = "tabModels";
			this.tabModels.Padding = new System.Windows.Forms.Padding(3);
			this.tabModels.Size = new System.Drawing.Size(556, 467);
			this.tabModels.TabIndex = 0;
			this.tabModels.Text = "Models";
			this.tabModels.UseVisualStyleBackColor = true;
			// 
			// tabEntities
			// 
			this.tabEntities.Location = new System.Drawing.Point(4, 22);
			this.tabEntities.Name = "tabEntities";
			this.tabEntities.Padding = new System.Windows.Forms.Padding(3);
			this.tabEntities.Size = new System.Drawing.Size(556, 467);
			this.tabEntities.TabIndex = 1;
			this.tabEntities.Text = "Entities";
			this.tabEntities.UseVisualStyleBackColor = true;
			// 
			// tabItems
			// 
			this.tabItems.Location = new System.Drawing.Point(4, 22);
			this.tabItems.Name = "tabItems";
			this.tabItems.Padding = new System.Windows.Forms.Padding(3);
			this.tabItems.Size = new System.Drawing.Size(556, 467);
			this.tabItems.TabIndex = 7;
			this.tabItems.Text = "Items";
			this.tabItems.UseVisualStyleBackColor = true;
			// 
			// tabFX
			// 
			this.tabFX.Location = new System.Drawing.Point(4, 22);
			this.tabFX.Name = "tabFX";
			this.tabFX.Padding = new System.Windows.Forms.Padding(3);
			this.tabFX.Size = new System.Drawing.Size(556, 467);
			this.tabFX.TabIndex = 3;
			this.tabFX.Text = "FX";
			this.tabFX.UseVisualStyleBackColor = true;
			// 
			// tabDecals
			// 
			this.tabDecals.Location = new System.Drawing.Point(4, 22);
			this.tabDecals.Name = "tabDecals";
			this.tabDecals.Padding = new System.Windows.Forms.Padding(3);
			this.tabDecals.Size = new System.Drawing.Size(556, 467);
			this.tabDecals.TabIndex = 6;
			this.tabDecals.Text = "Decals";
			this.tabDecals.UseVisualStyleBackColor = true;
			// 
			// tabMegatexture
			// 
			this.tabMegatexture.Location = new System.Drawing.Point(4, 22);
			this.tabMegatexture.Name = "tabMegatexture";
			this.tabMegatexture.Padding = new System.Windows.Forms.Padding(3);
			this.tabMegatexture.Size = new System.Drawing.Size(556, 467);
			this.tabMegatexture.TabIndex = 4;
			this.tabMegatexture.Text = "Megatexture";
			this.tabMegatexture.UseVisualStyleBackColor = true;
			// 
			// tabMap
			// 
			this.tabMap.Location = new System.Drawing.Point(4, 22);
			this.tabMap.Name = "tabMap";
			this.tabMap.Padding = new System.Windows.Forms.Padding(3);
			this.tabMap.Size = new System.Drawing.Size(556, 467);
			this.tabMap.TabIndex = 2;
			this.tabMap.Text = "Map Editor";
			this.tabMap.UseVisualStyleBackColor = true;
			// 
			// EditorForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(570, 570);
			this.Controls.Add(this.mainTabs);
			this.Controls.Add(this.menuStrip1);
			this.Controls.Add(this.panel1);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MainMenuStrip = this.menuStrip1;
			this.MinimumSize = new System.Drawing.Size(400, 400);
			this.Name = "EditorForm";
			this.Text = "Fusion Console";
			this.panel1.ResumeLayout(false);
			this.mainTabs.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button buttonExit;
		private System.Windows.Forms.Button buttonBuild;
		private System.Windows.Forms.TabControl mainTabs;
		private System.Windows.Forms.TabPage tabModels;
		private System.Windows.Forms.TabPage tabEntities;
		private System.Windows.Forms.TabPage tabMap;
		private System.Windows.Forms.TabPage tabFX;
		private System.Windows.Forms.TabPage tabMegatexture;
		private System.Windows.Forms.TabPage tabConfig;
		private System.Windows.Forms.TabPage tabDecals;
		private System.Windows.Forms.TabPage tabItems;
	}
}