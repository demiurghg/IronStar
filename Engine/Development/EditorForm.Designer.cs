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
			this.tabDashboard = new System.Windows.Forms.TabPage();
			this.label2 = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			this.maskedTextBox1 = new System.Windows.Forms.MaskedTextBox();
			this.maskedTextBox2 = new System.Windows.Forms.MaskedTextBox();
			this.panel2 = new System.Windows.Forms.Panel();
			this.button2 = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.checkBox1 = new System.Windows.Forms.CheckBox();
			this.label5 = new System.Windows.Forms.Label();
			this.button3 = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.button4 = new System.Windows.Forms.Button();
			this.label7 = new System.Windows.Forms.Label();
			this.button5 = new System.Windows.Forms.Button();
			this.button6 = new System.Windows.Forms.Button();
			this.button7 = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.button8 = new System.Windows.Forms.Button();
			this.button9 = new System.Windows.Forms.Button();
			this.versionLabel = new System.Windows.Forms.Label();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.button10 = new System.Windows.Forms.Button();
			this.panel1.SuspendLayout();
			this.mainTabs.SuspendLayout();
			this.tabDashboard.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(538, 24);
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
			this.panel1.Size = new System.Drawing.Size(538, 44);
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
			this.buttonBuild.Location = new System.Drawing.Point(434, 11);
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
			this.mainTabs.Controls.Add(this.tabDashboard);
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
			this.mainTabs.Size = new System.Drawing.Size(532, 493);
			this.mainTabs.TabIndex = 5;
			// 
			// tabConfig
			// 
			this.tabConfig.Location = new System.Drawing.Point(4, 22);
			this.tabConfig.Name = "tabConfig";
			this.tabConfig.Padding = new System.Windows.Forms.Padding(3);
			this.tabConfig.Size = new System.Drawing.Size(470, 467);
			this.tabConfig.TabIndex = 5;
			this.tabConfig.Text = "Config";
			this.tabConfig.UseVisualStyleBackColor = true;
			// 
			// tabModels
			// 
			this.tabModels.Location = new System.Drawing.Point(4, 22);
			this.tabModels.Name = "tabModels";
			this.tabModels.Padding = new System.Windows.Forms.Padding(3);
			this.tabModels.Size = new System.Drawing.Size(470, 467);
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
			// tabDashboard
			// 
			this.tabDashboard.Controls.Add(this.button10);
			this.tabDashboard.Controls.Add(this.textBox1);
			this.tabDashboard.Controls.Add(this.versionLabel);
			this.tabDashboard.Controls.Add(this.label1);
			this.tabDashboard.Controls.Add(this.button5);
			this.tabDashboard.Controls.Add(this.button7);
			this.tabDashboard.Controls.Add(this.button6);
			this.tabDashboard.Controls.Add(this.button9);
			this.tabDashboard.Controls.Add(this.button8);
			this.tabDashboard.Controls.Add(this.button4);
			this.tabDashboard.Controls.Add(this.label7);
			this.tabDashboard.Controls.Add(this.button3);
			this.tabDashboard.Controls.Add(this.checkBox1);
			this.tabDashboard.Controls.Add(this.button2);
			this.tabDashboard.Controls.Add(this.label4);
			this.tabDashboard.Controls.Add(this.panel2);
			this.tabDashboard.Controls.Add(this.maskedTextBox2);
			this.tabDashboard.Controls.Add(this.maskedTextBox1);
			this.tabDashboard.Controls.Add(this.button1);
			this.tabDashboard.Controls.Add(this.label5);
			this.tabDashboard.Controls.Add(this.label3);
			this.tabDashboard.Controls.Add(this.label2);
			this.tabDashboard.Location = new System.Drawing.Point(4, 22);
			this.tabDashboard.Name = "tabDashboard";
			this.tabDashboard.Padding = new System.Windows.Forms.Padding(3);
			this.tabDashboard.Size = new System.Drawing.Size(524, 467);
			this.tabDashboard.TabIndex = 8;
			this.tabDashboard.Text = "Dashboard";
			this.tabDashboard.UseVisualStyleBackColor = true;
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(186, 141);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(113, 24);
			this.label2.TabIndex = 0;
			this.label2.Text = "IP Address : Port";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(305, 171);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(181, 24);
			this.button1.TabIndex = 1;
			this.button1.Text = "Connect";
			this.button1.UseVisualStyleBackColor = true;
			// 
			// maskedTextBox1
			// 
			this.maskedTextBox1.Culture = new System.Globalization.CultureInfo("");
			this.maskedTextBox1.Location = new System.Drawing.Point(305, 145);
			this.maskedTextBox1.Mask = "255.255.255.255";
			this.maskedTextBox1.Name = "maskedTextBox1";
			this.maskedTextBox1.Size = new System.Drawing.Size(100, 20);
			this.maskedTextBox1.TabIndex = 2;
			// 
			// maskedTextBox2
			// 
			this.maskedTextBox2.Culture = new System.Globalization.CultureInfo("");
			this.maskedTextBox2.Location = new System.Drawing.Point(411, 145);
			this.maskedTextBox2.Mask = "255.255.255.255";
			this.maskedTextBox2.Name = "maskedTextBox2";
			this.maskedTextBox2.Size = new System.Drawing.Size(75, 20);
			this.maskedTextBox2.TabIndex = 2;
			// 
			// panel2
			// 
			this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
			this.panel2.Location = new System.Drawing.Point(0, 0);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(180, 467);
			this.panel2.TabIndex = 3;
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(305, 53);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(181, 24);
			this.button2.TabIndex = 6;
			this.button2.Text = "Start Server";
			this.button2.UseVisualStyleBackColor = true;
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(186, 24);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(113, 24);
			this.label4.TabIndex = 5;
			this.label4.Text = "Map";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// checkBox1
			// 
			this.checkBox1.AutoSize = true;
			this.checkBox1.Location = new System.Drawing.Point(411, 31);
			this.checkBox1.Name = "checkBox1";
			this.checkBox1.Size = new System.Drawing.Size(75, 17);
			this.checkBox1.TabIndex = 9;
			this.checkBox1.Text = "Dedicated";
			this.checkBox1.UseVisualStyleBackColor = true;
			// 
			// label5
			// 
			this.label5.BackColor = System.Drawing.SystemColors.MenuBar;
			this.label5.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.label5.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
			this.label5.Location = new System.Drawing.Point(186, 3);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(332, 21);
			this.label5.TabIndex = 0;
			this.label5.Text = "Server Control";
			this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(305, 84);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(181, 24);
			this.button3.TabIndex = 10;
			this.button3.Text = "Kill Server";
			this.button3.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			this.label3.BackColor = System.Drawing.SystemColors.Menu;
			this.label3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.label3.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
			this.label3.Location = new System.Drawing.Point(186, 121);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(332, 21);
			this.label3.TabIndex = 0;
			this.label3.Text = "Client Control";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// button4
			// 
			this.button4.Location = new System.Drawing.Point(305, 233);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(163, 24);
			this.button4.TabIndex = 14;
			this.button4.Text = "Save Configuration";
			this.button4.UseVisualStyleBackColor = true;
			// 
			// label7
			// 
			this.label7.BackColor = System.Drawing.SystemColors.MenuBar;
			this.label7.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.label7.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
			this.label7.Location = new System.Drawing.Point(186, 209);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(332, 21);
			this.label7.TabIndex = 12;
			this.label7.Text = "Configuration";
			this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// button5
			// 
			this.button5.Location = new System.Drawing.Point(305, 263);
			this.button5.Name = "button5";
			this.button5.Size = new System.Drawing.Size(163, 24);
			this.button5.TabIndex = 14;
			this.button5.Text = "Save Configuration";
			this.button5.UseVisualStyleBackColor = true;
			// 
			// button6
			// 
			this.button6.Location = new System.Drawing.Point(474, 233);
			this.button6.Name = "button6";
			this.button6.Size = new System.Drawing.Size(32, 24);
			this.button6.TabIndex = 14;
			this.button6.Text = "...";
			this.button6.UseVisualStyleBackColor = true;
			// 
			// button7
			// 
			this.button7.Location = new System.Drawing.Point(474, 263);
			this.button7.Name = "button7";
			this.button7.Size = new System.Drawing.Size(32, 24);
			this.button7.TabIndex = 14;
			this.button7.Text = "...";
			this.button7.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.SystemColors.MenuBar;
			this.label1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.label1.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
			this.label1.Location = new System.Drawing.Point(186, 301);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(332, 21);
			this.label1.TabIndex = 15;
			this.label1.Text = "Content";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// button8
			// 
			this.button8.Location = new System.Drawing.Point(305, 325);
			this.button8.Name = "button8";
			this.button8.Size = new System.Drawing.Size(100, 24);
			this.button8.TabIndex = 14;
			this.button8.Text = "Build";
			this.button8.UseVisualStyleBackColor = true;
			// 
			// button9
			// 
			this.button9.Location = new System.Drawing.Point(411, 325);
			this.button9.Name = "button9";
			this.button9.Size = new System.Drawing.Size(100, 24);
			this.button9.TabIndex = 14;
			this.button9.Text = "Rebuild";
			this.button9.UseVisualStyleBackColor = true;
			// 
			// versionLabel
			// 
			this.versionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.versionLabel.Location = new System.Drawing.Point(186, 441);
			this.versionLabel.Name = "versionLabel";
			this.versionLabel.Size = new System.Drawing.Size(332, 23);
			this.versionLabel.TabIndex = 16;
			this.versionLabel.Text = "IronStar v0.1 (Release)";
			this.versionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(305, 27);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(100, 20);
			this.textBox1.TabIndex = 17;
			// 
			// button10
			// 
			this.button10.Location = new System.Drawing.Point(305, 355);
			this.button10.Name = "button10";
			this.button10.Size = new System.Drawing.Size(100, 24);
			this.button10.TabIndex = 18;
			this.button10.Text = "Reload";
			this.button10.UseVisualStyleBackColor = true;
			// 
			// EditorForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(538, 570);
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
			this.tabDashboard.ResumeLayout(false);
			this.tabDashboard.PerformLayout();
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
		private System.Windows.Forms.TabPage tabDashboard;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button button5;
		private System.Windows.Forms.Button button7;
		private System.Windows.Forms.Button button6;
		private System.Windows.Forms.Button button9;
		private System.Windows.Forms.Button button8;
		private System.Windows.Forms.Button button4;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.CheckBox checkBox1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.MaskedTextBox maskedTextBox2;
		private System.Windows.Forms.MaskedTextBox maskedTextBox1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button button10;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Label versionLabel;
	}
}