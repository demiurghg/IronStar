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
		class UndoCmd : ICommand 
		{
			readonly Invoker invoker;

			public UndoCmd ( Invoker invoker )
			{
				this.invoker = invoker;
			}

			public object Execute()
			{
				if (!invoker.UndoInternal(1))
				{
					Log.Warning("Undo stack is empty");
				}
				return null;
			}
		}
	}
}
