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
			readonly int count;
		
			public RedoCmd ( Invoker invoker, ArgList args )
			{
				this.invoker = invoker;

				if (args.Count>1) {
					count = MathUtil.Clamp( int.Parse(args[1]), 0, int.MaxValue );
				}
			}


			public override object Execute()
			{
				invoker.Redo(1);
				return null;
			}
		}
	}
}
