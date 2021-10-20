using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;
using IronStar.ECS;
using IronStar.Mapping;

namespace IronStar.Editor.Commands
{
	public abstract class BaseCommand
	{
		protected readonly MapEditor editor;
		protected readonly IGameState gs;
		protected readonly Map map;

		private readonly MapNode[] storedSelection;

		protected IEnumerable<MapNode> Selection 
		{
			get { return storedSelection; }
		}

		public BaseCommand( MapEditor editor )
		{
			this.editor	=	editor;
			this.gs		=	editor.GameState;
			this.map	=	editor.Map;

			//	save selection :
			storedSelection = editor.Selection.ToArray();
		}

		protected void RestoreSelection()
		{
			editor.Selection.SetRange( storedSelection );
		}
	}
}
