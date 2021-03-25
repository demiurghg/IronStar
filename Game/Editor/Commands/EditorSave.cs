using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;

namespace IronStar.Editor.Commands
{
	class EditorSave : ICommand
	{
		readonly MapEditor mapEditor;

		public EditorSave ( MapEditor mapEditor )
		{
			this.mapEditor	=	mapEditor;
		}

		public object Execute()
		{
			mapEditor.SaveMap();
			return null;
		}
	}
}
