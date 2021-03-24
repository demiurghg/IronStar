using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;
using IronStar.Mapping;

namespace IronStar.Editor.Commands
{
	public class DuplicateSelected : BaseCommand
	{
		readonly MapNode[] duplicates;

		public DuplicateSelected( MapEditor editor ) : base(editor)
		{
			duplicates	=	Selection
							.Select( n => n.DuplicateNode() )
							.ToArray();
		}

		public override object Execute()
		{
			foreach ( var node in duplicates ) 
			{
				editor.Map.Nodes.Add( node );
				node.SpawnNodeECS( editor.GameState );
			}

			editor.Selection.SetRange( duplicates );

			return null;
		}

		public override void Rollback()
		{
			foreach ( var node in duplicates ) 
			{
				node.KillNodeECS( editor.GameState );
				editor.Map.Nodes.Remove( node );
			}

			RestoreSelection();
		}
	}
}
