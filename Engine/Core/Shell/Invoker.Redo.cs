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

namespace Fusion.Core.Shell 
{
	public partial class Invoker 
	{
		class RedoCmd : ICommand 
		{
			readonly Invoker invoker;

			public RedoCmd ( Invoker invoker )
			{
				this.invoker = invoker;
			}

			public object Execute()
			{
				if (!invoker.RedoInternal(1))
				{
					Log.Warning("Redo stack is empty");
				}
				return null;
			}
		}
	}
}
