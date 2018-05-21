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

		class Get : CommandNoHistory {

			readonly Invoker invoker;
			readonly string variable;
			readonly bool print;

		
			public Get ( Invoker invoker, string variable, bool print )
			{
				this.invoker = invoker;
				this.variable = variable;
				this.print	= print;
			}


			public Get ( Invoker invoker, ArgList args )
			{
				this.invoker = invoker;

				args.Usage("set <variable> /print")
					.Require( "variable"	,	out variable	)
					.Option	( "/print"		, out print )
					.Apply();
			}


			public override object Execute()
			{
				var propValue = invoker.GetComponentProperty(variable);

				if (print) {
					Log.Message("{0} = {1}", variable, propValue);
					return null;
				} else {
					return propValue;
				}
			}
		}
	}
}
