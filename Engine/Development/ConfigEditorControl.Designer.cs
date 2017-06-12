namespace Fusion.Development {
	partial class ConfigEditorControl {
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.configPropertyGrid = new System.Windows.Forms.PropertyGrid();
			this.panel2 = new System.Windows.Forms.Panel();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this.configListBox = new System.Windows.Forms.ListBox();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// configPropertyGrid
			// 
			this.configPropertyGrid.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.configPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.configPropertyGrid.Location = new System.Drawing.Point(200, 24);
			this.configPropertyGrid.Name = "configPropertyGrid";
			this.configPropertyGrid.Size = new System.Drawing.Size(242, 493);
			this.configPropertyGrid.TabIndex = 4;
			this.configPropertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.mainPropertyGrid_PropertyValueChanged);
			// 
			// panel2
			// 
			this.panel2.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.panel2.Controls.Add(this.configPropertyGrid);
			this.panel2.Controls.Add(this.splitter1);
			this.panel2.Controls.Add(this.configListBox);
			this.panel2.Controls.Add(this.menuStrip1);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel2.Location = new System.Drawing.Point(0, 0);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(442, 517);
			this.panel2.TabIndex = 9;
			// 
			// splitter1
			// 
			this.splitter1.Location = new System.Drawing.Point(197, 24);
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size(3, 493);
			this.splitter1.TabIndex = 7;
			this.splitter1.TabStop = false;
			// 
			// configListBox
			// 
			this.configListBox.Dock = System.Windows.Forms.DockStyle.Left;
			this.configListBox.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.configListBox.FormattingEnabled = true;
			this.configListBox.IntegralHeight = false;
			this.configListBox.ItemHeight = 15;
			this.configListBox.Location = new System.Drawing.Point(0, 24);
			this.configListBox.Name = "configListBox";
			this.configListBox.Size = new System.Drawing.Size(197, 493);
			this.configListBox.TabIndex = 5;
			this.configListBox.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
			// 
			// menuStrip1
			// 
			this.menuStrip1.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(442, 24);
			this.menuStrip1.TabIndex = 6;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// ConfigEditorControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.panel2);
			this.Name = "ConfigEditorControl";
			this.Size = new System.Drawing.Size(442, 517);
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.PropertyGrid configPropertyGrid;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Splitter splitter1;
		private System.Windows.Forms.ListBox configListBox;
		private System.Windows.Forms.MenuStrip menuStrip1;
	}
}
