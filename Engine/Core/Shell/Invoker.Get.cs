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

		
			public Get ( Invoker invoker, ArgList args )
			{
				this.invoker = invoker;

				args.Usage("set <variable>f")
					.Require( "variable"	,	out variable	)
					.Apply();
			}


			public override object Execute()
			{
				IGameComponent component;
				PropertyInfo pi;

				if (!invoker.TryGetComponentProperty( variable, out pi, out component )) {
					throw new InvokerException("bad component property name '{0}'", variable);
				}

				var propValue = pi.GetValue( component );

				return propValue;
			}
		}
	}
}
