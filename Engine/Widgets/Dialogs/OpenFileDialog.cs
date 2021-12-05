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
	public class OpenFileDialog : FileDialog
	{
		Action<string> openAction;

		public OpenFileDialog( UIState ui, string defaultDir, string searchPattern ) : base(ui, defaultDir, searchPattern)
		{
		}


		public void Show( Action<string> openAction )
		{
			this.openAction = openAction;
			ShowInternal();
		}


		protected override string ButtonName
		{
			get { return "Open"; }
		}


		protected override bool Accept( string fullPath, string relativePath, bool fileExists )
		{
			if (!fileExists)
			{
				MessageBox.ShowError(ui, string.Format("File {0} does not exists", relativePath), null );
				return false;
			}
			else
			{
				openAction?.Invoke( relativePath );
				return true;
			}
		}
	}
}
