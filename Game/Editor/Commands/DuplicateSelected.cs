﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;
using IronStar.Mapping;

namespace IronStar.Editor.Commands
{
	public class DuplicateSelected : BaseCommand, IUndoable
	{
		readonly MapNode[] duplicates;

		public DuplicateSelected( MapEditor editor ) : base(editor)
		{
			duplicates	=	Selection
							.Select( n => n.DuplicateNode() )
							.ToArray();
		}

		public virtual object Execute()
		{
			foreach ( var node in duplicates ) 
			{
				node.Name = editor.GetUniqueName( node );
				editor.Map.Nodes.Add( node );
				node.SpawnNodeECS( editor.GameState );
			}

			editor.Selection.SetRange( duplicates );

			return null;
		}

		public virtual void Rollback()
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
