using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Core.Shell;
using IronStar.ECS;
using IronStar.Mapping;
using Fusion;

namespace IronStar.Editor.Commands
{
	public class BakeCommand : BaseCommand, IUndoable
	{
		class RollbackInfo
		{
			public MapNode		Node;
			public Vector3		Translation;
			public Quaternion	Rotation;
		}
		
		readonly RollbackInfo[] rollbackInfo;
		public Vector3 MoveVector;
		
		public BakeCommand( MapEditor editor ) : base(editor)
		{
			this.MoveVector		=	Vector3.Zero;
			this.rollbackInfo	=	editor
								.Selection
								.Where(	obj0 => obj0 is MapNode )
								.Select( obj2 => new RollbackInfo { Node = obj2, Translation = obj2.Translation, Rotation = obj2.Rotation } )
								.ToArray();
		}


		public virtual object Execute()
		{
			foreach ( var ri in rollbackInfo )
			{
				var node = ri.Node;
				var transform = ( node as MapEntity)?.EcsEntity?.GetComponent<ECS.KinematicState>();

				try 
				{
					if (transform!=null)
					{
						ri.Node.Translation	= transform.Position;
						ri.Node.Rotation	= transform.Rotation;
						ri.Node.ResetNodeECS(gs);
					}
				} 
				catch ( Exception e )
				{
					Log.Warning("Failed to bake transform for {0}: {1}", node.Name, e.Message);
					ri.Node.Translation	= ri.Translation;
					ri.Node.Rotation	= ri.Rotation;
					ri.Node.ResetNodeECS(gs);
				}
			}
			return null;
		}

		
		public virtual void Rollback()
		{
			foreach ( var ri in rollbackInfo )
			{
				ri.Node.Translation	=	ri.Translation;
				ri.Node.Rotation	=	ri.Rotation;
				ri.Node.ResetNodeECS(gs);
			}
			RestoreSelection();
		}
	}
}
