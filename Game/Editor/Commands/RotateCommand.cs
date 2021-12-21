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
using Fusion;

namespace IronStar.Editor.Commands
{
	public class RotateCommand : BaseCommand, IUndoable
	{
		class RollbackInfo
		{
			public MapNode Node;
			public Quaternion Rotation;
		}
		
		readonly RollbackInfo[] rollbackInfo;
		public Quaternion Rotation = Quaternion.Identity;


		public RotateCommand( MapEditor editor ) : base(editor)
		{
			this.rollbackInfo	=	editor
								.Selection
								.Where(	obj0 => obj0 is MapNode )
								.Select( obj2 => new RollbackInfo { 
									Node	= obj2, 
									Rotation= obj2.Rotation,
								} )
								.ToArray();
		}


		public virtual object Execute()
		{
			foreach ( var ri in rollbackInfo )
			{
				ri.Node.Rotation	=	Rotation * ri.Rotation;
				ri.Node.ResetNodeECS(gs);
			}
			return null;
		}

		
		public virtual void Rollback()
		{
			foreach ( var ri in rollbackInfo )
			{
				ri.Node.Rotation	=	ri.Rotation;
				ri.Node.ResetNodeECS(gs);
			}
			RestoreSelection();
		}
	}
}
