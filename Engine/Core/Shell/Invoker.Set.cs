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
			readonly string variable;
			readonly string value;
			readonly bool historyoff;

			string rollback = null;

			///		set RenderSystem.VSyncInterval 1
			///		set GameServer.UpdateRate 60

		
			public Set ( Invoker invoker, ArgList args )
			{
				this.invoker = invoker;

				args.Usage("set <variable> value /noundo /rollback:value /historyoff")
					.Require( "variable"	,	out variable	)
					.Require( "value"		,	out value		)
					.Option	( "rollback"	,	out rollback	)
					.Option	( "historyoff"	,	out historyoff	)
					.Apply();
			}


			public object Execute()
			{
				IGameComponent component;
				PropertyInfo pi;

				if (!invoker.TryGetComponentProperty( variable, out pi, out component )) {
					throw new InvokerException("bad component property name '{0}'", variable);
				}

				object newVal;

				if (!StringConverter.TryConvertFromString(pi.PropertyType, value, out newVal )) {
					throw new InvokerException("can not set {0} from '{1}'", pi.PropertyType, value);
				}

				pi.SetValue( component, newVal );

				return null;
			}

			public bool IsHistoryOn()
			{
				return false;
			}

			public void Rollback()
			{
				throw new NotImplementedException();
			}
		}
	}
}
