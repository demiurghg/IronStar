using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;
using IronStar.Mapping;

namespace IronStar.Editor.Commands
{
	public class CreateNode : BaseCommand, IUndoable
	{
		readonly MapNode newNode;

		public CreateNode( MapEditor editor, MapNode newNode ) : base(editor)
		{
			this.newNode	=	newNode;
		}

		public virtual object Execute()
		{
			newNode.Name	=	editor.GetUniqueName(newNode); 

			map.Nodes.Add( newNode );
			newNode.SpawnNodeECS( gs );
			editor.Selection.Set( newNode );

			return null;
		}

		public virtual void Rollback()
		{
			map.Nodes.Remove( newNode );
			newNode.KillNodeECS( gs );

			RestoreSelection();
		}
	}
}
