using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using System.Reflection;
using Fusion.Core.Extensions;
using System.Runtime.CompilerServices;

namespace Fusion.Core.Shell {
	public partial class Invoker {

		string[] commandNames = null;
		string[] variableNames = null;

		void FlushNameCache ()
		{
			lock (lockObject) {
				commandNames = null;
				variableNames = null;
			}
		}


		string[] GetCommandNameList ()
		{
			lock (lockObject) {
				if (commandNames==null) {
					commandNames =	commandsRegistry
						.OrderBy( p1 => p1.Key )
						.Select( p2 => p2.Key )
						.ToArray();
				}
				return commandNames;
			}
		}


		string[] GetVariableNameList ()
		{
			lock (lockObject) {
				if (variableNames==null) {

					var list = new List<string>();

					foreach ( var component in Game.Components ) {

						var componentName = component.GetType().Name;

						var varList = component
									.GetType()
									.GetProperties()
									.Where( p1 => p1.HasAttribute<ConfigAttribute>() )
									.Select( p2 => componentName + "." + p2.Name )
									.ToArray();
				
						list.AddRange( varList );
					}
					variableNames = list.ToArray();
				}
				return variableNames;
			}
		}
	}
}
