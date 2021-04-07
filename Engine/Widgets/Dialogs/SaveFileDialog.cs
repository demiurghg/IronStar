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
using Fusion.Engine.Frames.Layouts;

namespace Fusion.Widgets.Dialogs 
{
	public class SaveFileDialog : FileDialog
	{
		Action<string> saveAction;

		public SaveFileDialog( FrameProcessor fp, string defaultDir, string searchPattern ) : base(fp, defaultDir, searchPattern)
		{
		}


		public void Show( Action<string> saveAction, string initialFileName = null )
		{
			this.saveAction = saveAction;
			ShowInternal();
		}


		protected override string ButtonName
		{
			get { return "Save"; }
		}


		protected override bool Accept( string fullPath, string relativePath, bool fileExists )
		{
			if (fileExists)
			{
				MessageBox.ShowQuestion(Frames, 
					string.Format("File {0} already exists. Overwrite?", relativePath), 
					() => { 
						saveAction(relativePath); 
						Close();
					}, 
					null 
				);
				return false;
			}
			else
			{
				saveAction?.Invoke( relativePath );
				return true;
			}
		}
	}
}
