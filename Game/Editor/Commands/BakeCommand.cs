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
	public class BakeCommand : BaseCommand
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
								.Select( obj2 => new RollbackInfo { Node = obj2, Translation = obj2.TranslateVector, Rotation = obj2.RotateQuaternion } )
								.ToArray();
		}


		public override object Execute()
		{
			foreach ( var ri in rollbackInfo )
			{
				var node = ri.Node;
				var transform = ( node as MapEntity)?.EcsEntity?.GetComponent<ECS.Transform>();

				try 
				{
					if (transform!=null)
					{
						ri.Node.TranslateVector = transform.Position;
						ri.Node.RotateQuaternion = transform.Rotation;
						ri.Node.ResetNodeECS(gs);
					}
				} 
				catch ( Exception e )
				{
					Log.Warning("Failed to bake transform for {0}: {1}", node.Name, e.Message);
					ri.Node.TranslateVector	 = ri.Translation;
					ri.Node.RotateQuaternion = ri.Rotation;
					ri.Node.ResetNodeECS(gs);
				}
			}
			return null;
		}

		
		public override void Rollback()
		{
			foreach ( var ri in rollbackInfo )
			{
				ri.Node.TranslateVector	 = ri.Translation;
				ri.Node.RotateQuaternion = ri.Rotation;
				ri.Node.ResetNodeECS(gs);
			}
			RestoreSelection();
		}
	}
}
