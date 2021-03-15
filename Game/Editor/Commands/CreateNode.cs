using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;
using IronStar.Mapping;

namespace IronStar.Editor.Commands
{
	public class CreateNode : BaseCommand
	{
		readonly MapNode newNode;

		public CreateNode( MapEditor editor, MapNode newNode ) : base(editor)
		{
			this.newNode	=	newNode;
		}

		public override object Execute()
		{
			map.Nodes.Add( newNode );
			newNode.SpawnNodeECS( gs );
			editor.Selection.Set( newNode );

			return null;
		}

		public override void Rollback()
		{
			map.Nodes.Remove( newNode );
			newNode.KillNodeECS( gs );

			RestoreSelection();
		}
	}
}
