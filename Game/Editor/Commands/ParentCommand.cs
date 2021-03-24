﻿using System;
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
	public class ParentCommand : BaseCommand
	{
		MapNode parentNode = null;
		RollbackInfo[] rollbackInfo;
		
		class RollbackInfo
		{
			public MapNode Node;
			public MapNode OldParent;
			public Matrix GlobalTransform;
		}
		

		public ParentCommand( MapEditor editor ) : base(editor)
		{
			if (Selection.Any())
			{
				parentNode	=	Selection.Last();
			}

			rollbackInfo	=	Selection
								.Select( node => new RollbackInfo 
								{ 
									Node = node, 
									OldParent = node.Parent, 
									GlobalTransform = node.GlobalTransform
								})
								.ToArray();
		}


		public override object Execute()
		{
			foreach ( var ri in rollbackInfo )
			{
				if (ri.Node!=parentNode)
				{
					ri.Node.Parent			=	parentNode;
					ri.Node.GlobalTransform	=	ri.GlobalTransform;
				}

				ri.Node.ResetNodeECS(gs);
			}
			return null;
		}

		
		public override void Rollback()
		{
			foreach ( var ri in rollbackInfo )
			{
				ri.Node.Parent = ri.OldParent;
				ri.Node.ResetNodeECS(gs);
			}
			RestoreSelection();
		}
	}
}
