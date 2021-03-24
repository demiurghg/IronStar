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

		protected IEnumerable<MapNode> SelectionHierarchy
		{
			get 
			{
				Queue<MapNode> Q = new Queue<MapNode>();
				List<MapNode> list = new List<MapNode>();

				foreach (var node in Selection)
				{
					Q.Enqueue( node );

					while ( Q.Any() ) 
					{
						var t = Q.Dequeue();
						list.Add( t );

						foreach ( var u in t.Children ) 
						{
							Q.Enqueue( u );
						}
					}
				}

				return list;
			}
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
