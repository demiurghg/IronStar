using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;
using IronStar.Mapping;

namespace IronStar.Editor.Commands
{
	public class DeleteSelected : BaseCommand
	{
		public DeleteSelected( MapEditor editor ) : base(editor)
		{
		}

		public override object Execute()
		{
			foreach ( var node in Selection ) 
			{
				node.KillNodeECS( gs );
				map.Nodes.Remove( node );
			}

			editor.Selection.RemoveRange( Selection );

			return null;
		}

		public override void Rollback()
		{
			foreach ( var node in Selection ) 
			{
				editor.Map.Nodes.Add( node );
				node.SpawnNodeECS( editor.GameState );
			}

			RestoreSelection();
		}
	}
}
