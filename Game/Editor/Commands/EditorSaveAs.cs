using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;

namespace IronStar.Editor.Commands
{
	class EditorSaveAs : ICommand
	{
		readonly MapEditor mapEditor;

		[CommandLineParser.Required]
		[CommandLineParser.Name("newMapName")]
		public string NewMapName { get; set; }

		public EditorSaveAs ( MapEditor mapEditor )
		{
			this.mapEditor	=	mapEditor;
		}

		public object Execute()
		{
			mapEditor.SaveMapAs(NewMapName);
			return null;
		}
	}
}
