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

	public class SelectNodes : BaseCommand
	{
		readonly MapNode[] items;
		readonly SelectMode mode;

		
		public SelectNodes( MapEditor editor, SelectMode mode, params MapNode[] nodes ) : base(editor)
		{
			this.mode	=	mode;
			this.items	=	nodes
							.Where(n => n!=null)
							.ToArray();
		}


		public override string ToString()
		{
			return string.Format("Select : {0} : {1} items", mode, items.Length );
		}


		public override object Execute()
		{
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

		
		public override void Rollback()
		{
			RestoreSelection();
		}
	}
}
