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
	public abstract class BaseCommand : IUndoable
	{
		protected readonly MapEditor editor;
		protected readonly GameState gs;
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

		public abstract object Execute();
		public abstract void Rollback();

		protected void RestoreSelection()
		{
			editor.Selection.SetRange( storedSelection );
		}
	}
}
