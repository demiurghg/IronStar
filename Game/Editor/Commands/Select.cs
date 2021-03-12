using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;
using IronStar.Mapping;

namespace IronStar.Editor.Commands
{
	public enum SelectMode
	{
		Replace,
		Append,
		Toggle,
		Clear,
	}

	public class Select : IUndoable
	{
		readonly MapNode[] items;
				 MapNode[] restore;
		readonly MapEditor editor;
		readonly SelectMode mode;

		
		public Select( MapEditor editor, SelectMode mode, params MapNode[] nodes )
		{
			this.mode	=	mode;
			this.editor	=	editor;

			this.items	=	nodes
							.Where(n => n!=null)
							.ToArray();
		}


		public override string ToString()
		{
			return string.Format("Select : {0} : {1} items", mode, items.Length );
		}


		public object Execute()
		{
			restore	=	editor.Selection.ToArray();

			switch (mode)
			{
				case SelectMode.Replace:
					editor.Selection.SetRange( items );
				break;

				case SelectMode.Append:
					editor.Selection.AddRange( items );
				break;

				case SelectMode.Toggle:
					editor.Selection.Toggle( items.FirstOrDefault() );
				break;

				case SelectMode.Clear:
					editor.Selection.Clear();
				break;
			}

			return null;
		}

		
		public void Rollback()
		{
			editor.Selection.SetRange( restore );
		}
	}
}
