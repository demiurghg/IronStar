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

		class Batch : ICommand {

			readonly ICommand[] commands;
			
			public Batch ( ICommand[] commands )
			{									
				this.commands = commands;
			}


			public bool IsHistoryOn ()
			{
				return true;
			}


			public object Execute()
			{
				int length = commands.Length;
				
				for (int i=0; i<length; i++) {

					var cmd = commands[i];

					cmd.Execute();
				}

				return null;
			}
		}
	}
}
