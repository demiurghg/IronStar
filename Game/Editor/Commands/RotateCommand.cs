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
	public class RotateCommand : BaseCommand, IUndoable
	{
		class RollbackInfo
		{
			public MapNode Node;
			public float Yaw;
			public float Pitch;
			public float Roll;
		}
		
		readonly RollbackInfo[] rollbackInfo;
		public float DeltaYaw = 0;
		public float DeltaPitch = 0;
		public float DeltaRoll = 0;
		
		public RotateCommand( MapEditor editor ) : base(editor)
		{
			this.rollbackInfo	=	editor
								.Selection
								.Where(	obj0 => obj0 is MapNode )
								.Select( obj2 => new RollbackInfo { 
									Node	= obj2, 
									Yaw		= obj2.RotateYaw, 
									Pitch	= obj2.RotatePitch, 
									Roll	= obj2.RotateRoll, 
								} )
								.ToArray();
		}


		public virtual object Execute()
		{
			foreach ( var ri in rollbackInfo )
			{
				ri.Node.RotateYaw	= ri.Yaw	+ DeltaYaw	;
				ri.Node.RotatePitch	= ri.Pitch	+ DeltaPitch;
				ri.Node.RotateRoll	= ri.Roll	+ DeltaRoll	;
				ri.Node.ResetNodeECS(gs);
			}
			return null;
		}

		
		public virtual void Rollback()
		{
			foreach ( var ri in rollbackInfo )
			{
				ri.Node.RotateYaw	= ri.Yaw	;
				ri.Node.RotatePitch	= ri.Pitch	;
				ri.Node.RotateRoll	= ri.Roll	;
				ri.Node.ResetNodeECS(gs);
			}
			RestoreSelection();
		}
	}
}
