using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;
using IronStar.Mapping;
using System.Reflection;

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
		object valueToSet;
		
		public SetProperty( MapEditor editor, PropertyInfo property ) : base(editor)
		{
			this.property		=	property;
			this.rollbackInfo	=	editor
								.Selection
								.Select( t => new RollbackInfo { Node = t, Value = property.GetValue(t) } )
								.ToArray();
		}


		public object ValueToSet
		{
			get { return valueToSet; }
			set 
			{
				valueToSet = value;
			}
		}


		public override object Execute()
		{
			foreach ( var ri in rollbackInfo )
			{
				property.SetValue( ri.Node, valueToSet );
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
