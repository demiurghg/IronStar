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

		class RedoCmd : CommandNoHistory {

			readonly Invoker invoker;

			[CommandLineParser.Option]
			[CommandLineParser.Name("count")]
			public int Count { get; set; } = 1;

			public RedoCmd ( Invoker invoker )
			{
				this.invoker = invoker;
			}


			public override object Execute()
			{
				invoker.Redo(Count);
				return null;
			}
		}
	}
}
