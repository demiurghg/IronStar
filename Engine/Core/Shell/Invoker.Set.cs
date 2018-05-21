using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Configuration;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using System.Diagnostics;
using Fusion.Engine;
using Fusion.Engine.Server;
using System.Reflection;
using System.ComponentModel;
using Fusion.Core.Mathematics;

namespace Fusion.Core.Shell {
	public partial class Invoker {

		class Set : ICommand {

			public class Syntax : ISyntax
			{
				public readonly Game game;

				public Syntax ( Game game )
				{
					this.game = game;
				}

				public string Description { get { return ""; } }
				public string Usage { get { return ""; } }

				public string[] Optional( string arg )
				{
					return new[] {"/historyoff", "/fast", };
				}

				public string[] Required(int index, string arg)
				{
					return new[] {"test1", "test2", "tet2222", "quake", "unreal", "quake5"};
				}
			}


			readonly Invoker invoker;
			readonly string variable;
			readonly string value;
			readonly bool historyoff;

			string oldValue = null;


			public Set ( Invoker invoker, string variable, string value )
			{
				this.invoker	=	invoker;
				this.value		=	value;
				this.variable	=	variable;
				this.historyoff	=	false;
			}

			
			public Set ( Invoker invoker, ArgList args )
			{
				this.invoker = invoker;

				args.Usage("set <variable> value /noundo /rollback:value /historyoff")
					.Require( "variable"	,	out variable	)
					.Require( "value"		,	out value		)
					.Option	( "/historyoff"	,	out historyoff	)
					.Apply();
			}


			public object Execute()
			{
				if (IsHistoryOn()) {
					oldValue = invoker.GetComponentProperty(variable);
				}

				invoker.SetComponentProperty( variable, value );
				return null;
			}

			public bool IsHistoryOn()
			{
				return !historyoff;
			}

			public void Rollback()
			{
				invoker.SetComponentProperty( variable, oldValue );
			}
		}
	}
}
