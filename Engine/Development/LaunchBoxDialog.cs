using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics;
using System.IO;
using System.Diagnostics;
using Fusion.Core;


namespace Fusion.Development {

	internal partial class LaunchBoxForm : Form {

		readonly Game game;

		string configPath;
		string configName;

		Action runEditor;


		public string StartupCommand 
		{
			get { return startupCommand.Text; }
		}


		public LaunchBoxForm ( Game game, string config, Action runEditor )
		{
			this.game	=	game;
			configName	=	config;
			configPath	=	game.UserStorage.GetFullPath(config);

			this.runEditor	=	runEditor;


			InitializeComponent();
			this.Icon	=	Fusion.Properties.Resources.fusionIconGrayscale;


			this.Text	=	game.GameTitle;

			UpdateControls();
		}



		void UpdateControls ()
		{

			//	version :
			versionLabel.Text	=	game.GetReleaseInfo();

			//	stereo mode :
			stereoMode.Items.Clear();
			stereoMode.Items.AddRange( Enum.GetValues(typeof(StereoMode)).Cast<object>().ToArray() );
			stereoMode.SelectedItem = RenderSystem.StereoMode;

			//	display mode :
			displayWidth.Value	=	RenderSystem.Width;
			displayHeight.Value	=	RenderSystem.Height;

			//	fullscreen
			fullscreen.Checked	=	RenderSystem.Fullscreen;

			//	track objects
			trackObjects.Checked	=	game.TrackObjects;

			//	use debug device :
			useRenderDoc.Checked	=	RenderSystem.UseRenderDoc;
			debugDevice.Checked		=	RenderSystem.UseDebugDevice;
		}



		private void button1_Click ( object sender, EventArgs e )
		{
			// stereo mode :
			RenderSystem.StereoMode	=	(StereoMode)stereoMode.SelectedItem;

			//	displya mode :
			RenderSystem.Width		=	(int)displayWidth.Value;
			RenderSystem.Height	=	(int)displayHeight.Value;

			//	fullscreen
			RenderSystem.Fullscreen	=	fullscreen.Checked;

			//	track objects
			game.TrackObjects	=	trackObjects.Checked;

			//	use debug device :
			RenderSystem.UseRenderDoc		=	useRenderDoc.Checked;
			RenderSystem.UseDebugDevice	=	debugDevice.Checked;

			if (!string.IsNullOrWhiteSpace(startupCommand.Text)) {
				game.Invoker.ExecuteString( startupCommand.Text );
			}

			this.DialogResult	=	DialogResult.OK;
			this.Close();
		}



		private void button3_Click ( object sender, EventArgs e )
		{
			this.Close();
		}



		void ShellExecute ( string path, bool wait = false )
		{
			ProcessStartInfo psi = new ProcessStartInfo(path);
			psi.UseShellExecute = true;
			var proc = Process.Start(psi);
			if (wait) {
				proc.WaitForExit();
			}
		}



		private void openConfig_Click ( object sender, EventArgs e )
		{
			ShellExecute( configPath, true );
			game.Config.LoadSettings(configName);
			UpdateControls();
		}



		private void openConfigDir_Click ( object sender, EventArgs e )
		{
			ShellExecute( Path.GetDirectoryName(configPath) );
		}



		private void openContent_Click ( object sender, EventArgs e )
		{
			var file = game.Invoker.ExecuteStringImmediate("contentFile") as string;
			ShellExecute(file);
		}



		private void openContentDir_Click ( object sender, EventArgs e )
		{
			var file = game.Invoker.ExecuteStringImmediate("contentFile") as string;
			ShellExecute(Path.GetDirectoryName(file));
		}



		private void buildContent_Click ( object sender, EventArgs e )
		{
			game.Invoker.ExecuteStringImmediate("contentBuild");
		}



		private void rebuildContent_Click ( object sender, EventArgs e )
		{
			var r = MessageBox.Show(this, 
							"Are you sure you want to rebuild entire content?", 
							"Rebuild Content", 
							MessageBoxButtons.YesNo, 
							MessageBoxIcon.Warning,
							MessageBoxDefaultButton.Button2);

			if (r==DialogResult.Yes) 
			{				
				game.Invoker.ExecuteStringImmediate("contentBuild /force");
			}
		}

		private void runEditorButton_Click( object sender, EventArgs e )
		{
			runEditor?.Invoke();
		}

		private void checkBox1_CheckedChanged( object sender, EventArgs e )
		{

		}
	}
}
