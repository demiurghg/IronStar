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

namespace Fusion.Core.Shell 
{
	public partial class Invoker 
	{
		string[] commandNames = null;
		string[] variableNames = null;

		string[] GetCommandNameList ()
		{
			lock (lockObject)
			{
				return commandsRegistry
					.OrderBy( p1 => p1.Key )
					.Select( p2 => p2.Key )
					.ToArray();
			}
		}


		string[] GetVariableNameList ()
		{
			lock (lockObject) 
			{
				var list = new List<string>();

				foreach (var config in configClasses.OrderBy(c=>c.Name))
				{
					var props = config.GetProperties(BindingFlags.Static|BindingFlags.Public);

					foreach (var prop in props.OrderBy(c=>c.Name))
					{
						list.Add( config.Name + "." + prop.Name );
					}
				}

				return list.ToArray();
			}
		}
	}
}
