using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;
using IronStar.Mapping;
using System.Reflection;
using Fusion.Core.Extensions;

namespace IronStar.Editor.Commands
{
	public class SetProperty : BaseCommand
	{
		class RollbackInfo
		{
			public MapNode Node;
			public object Value;
		}
		
		readonly PropertyInfo property;
		readonly RollbackInfo[] rollbackInfo;
		public object Value;
		
		public SetProperty( MapEditor editor, PropertyInfo property, object value ) : base(editor)
		{
			this.property		=	property;
			this.Value			=	value;
			this.rollbackInfo	=	editor
								.Selection
								.Where(	obj0 => obj0 is MapNode )
								.Where( obj1 => property.DeclaringType.IsAssignableFrom( obj1.GetType() ) )
								.Select( obj2 => new RollbackInfo { Node = obj2, Value = property.GetValue(obj2) } )
								.ToArray();
		}


		public override object Execute()
		{
			foreach ( var ri in rollbackInfo )
			{
				property.SetValue( ri.Node, Value );
				ri.Node.ResetNodeECS(gs);
			}
			return null;
		}

		
		public override void Rollback()
		{
			foreach ( var ri in rollbackInfo )
			{
				property.SetValue( ri.Node, ri.Value );
				ri.Node.ResetNodeECS(gs);
			}
			RestoreSelection();
		}
	}
}
