using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;
using IronStar.Mapping;
using System.Reflection;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;

namespace IronStar.Editor.Commands
{
	public class MoveCommand : BaseCommand
	{
		class RollbackInfo
		{
			public MapNode Node;
			public Vector3 Translation;
		}
		
		readonly RollbackInfo[] rollbackInfo;
		public Vector3 MoveVector;
		
		public MoveCommand( MapEditor editor ) : base(editor)
		{
			this.MoveVector		=	Vector3.Zero;
			this.rollbackInfo	=	editor
								.Selection
								.Where(	obj0 => obj0 is MapNode )
								.Select( obj2 => new RollbackInfo { Node = obj2, Translation = obj2.Translation } )
								.ToArray();
		}


		public override object Execute()
		{
			foreach ( var ri in rollbackInfo )
			{
				ri.Node.Translation = ri.Translation + MoveVector;
				ri.Node.ResetNodeECS(gs);
			}
			return null;
		}

		
		public override void Rollback()
		{
			foreach ( var ri in rollbackInfo )
			{
				ri.Node.Translation = ri.Translation;
				ri.Node.ResetNodeECS(gs);
			}
			RestoreSelection();
		}
	}
}
