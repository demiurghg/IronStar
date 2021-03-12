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
		class EchoCmd : ICommand 
		{
			readonly Invoker invoker;

			[CommandLineParser.Name("on")]
			[CommandLineParser.Option]
			public bool On { get; set; }

			[CommandLineParser.Name("off")]
			[CommandLineParser.Option]
			public bool Off { get; set; }

			public EchoCmd ( Invoker invoker )
			{
				this.invoker = invoker;
			}

			public object Execute()
			{
				if (On==Off)
				{
					invoker.Echo = !invoker.echo;
				}
				else
				{
					if (On) invoker.Echo = true;
					if (Off) invoker.Echo = false;
				}
				return null;
			}
		}
	}
}
