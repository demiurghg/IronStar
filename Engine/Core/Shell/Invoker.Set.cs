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

		class Toggle : ICommand {

			readonly Invoker invoker;

			[CommandLineParser.Required]
			[CommandLineParser.Name("variable")]
			public string Variable { get; set; }

			public Toggle ( Invoker invoker )
			{
				this.invoker = invoker;
			}


			public object Execute()
			{
				var value = invoker.GetComponentProperty(Variable);

				invoker.SetComponentProperty( Variable, (!StringConverter.ToBoolean(value)).ToString() );
				return null;
			}
		}
	}
}
