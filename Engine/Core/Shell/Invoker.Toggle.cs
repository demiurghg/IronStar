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

			readonly Invoker invoker;

			[CommandLineParser.Required]
			[CommandLineParser.Name("variable")]
			public string Variable { get; set; }

			[CommandLineParser.Required]
			[CommandLineParser.Name("value")]
			public string Value { get; set; }

			[CommandLineParser.Option]
			[CommandLineParser.Name("historyOff")]
			public bool HistoryOff { get; set; }

			string oldValue = null;


			public Set ( Invoker invoker, string variable, string value )
			{
				this.invoker	=	invoker;
				this.Value		=	value;
				this.Variable	=	variable;
				this.HistoryOff	=	false;
			}

			
			public Set ( Invoker invoker )
			{
				this.invoker = invoker;
			}


			public object Execute()
			{
				if (IsHistoryOn()) {
					oldValue = invoker.GetComponentProperty(Variable);
				}

				invoker.SetComponentProperty( Variable, Value );
				return null;
			}

			public bool IsHistoryOn()
			{
				return !HistoryOff;
			}

			public void Rollback()
			{
				invoker.SetComponentProperty( Variable, oldValue );
			}
		}
	}
}
