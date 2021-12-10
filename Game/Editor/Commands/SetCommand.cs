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
	/// <summary>
	/// Sets and rollback value for multiple selected map nodes.
	/// Support one nesting level: i.e. map node and first level nesting objects
	/// </summary>
	public class SetCommand : BaseCommand, IUndoable
	{
		class RollbackInfo
		{
			public MapNode Node;
			public object Value;
		}
		
		readonly PropertyInfo property;
		readonly PropertyInfo parentProperty;
		readonly RollbackInfo[] rollbackInfo;
		public object Value;
		
		public SetCommand( MapEditor editor, PropertyInfo parentProperty, PropertyInfo property, object value ) : base(editor)
		{
			this.property		=	property;
			this.parentProperty	=	parentProperty;
			this.Value			=	value;
			this.rollbackInfo	=	editor
								.Selection
								.Where(	obj0 => obj0 is MapNode )
								.Where( obj1 => (parentProperty??property).DeclaringType.IsAssignableFrom( obj1.GetType() ) )
								.Select( obj2 => new RollbackInfo { Node = obj2, Value = GetValue(obj2) } )
								.ToArray();
		}


		object GetValue(object node)
		{
			return property.GetValue( parentProperty?.GetValue(node) ?? node );
		}


		void SetValue(object node, object value)
		{
			property.SetValue( parentProperty?.GetValue(node) ?? node, value );
		}


		public virtual object Execute()
		{
			foreach ( var ri in rollbackInfo )
			{
				SetValue(ri.Node, Value);
				ri.Node.ResetNodeECS(gs);
			}
			return null;
		}

		
		public virtual void Rollback()
		{
			foreach ( var ri in rollbackInfo )
			{
				SetValue(ri.Node, ri.Value);
				ri.Node.ResetNodeECS(gs);
			}
			RestoreSelection();
		}
	}
}
