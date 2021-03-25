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
		class Get : ICommand 
		{
			readonly Invoker invoker;

			[CommandLineParser.Required]
			[CommandLineParser.Name("variable")]
			public string Variable { get; set; }

			[CommandLineParser.Option]
			[CommandLineParser.Name("print")]
			public bool Print { get; set; }

		
			public Get ( Invoker invoker, string variable, bool print )
			{
				this.invoker = invoker;
				this.Variable = variable;
				this.Print	= print;
			}


			public Get ( Invoker invoker )
			{
				this.invoker = invoker;
			}


			public object Execute()
			{
				var propValue = invoker.GetComponentProperty(Variable);

				if (Print) 
				{
					Log.Message("{0} = {1}", Variable, propValue);
					return null;
				}
				else 
				{
					return propValue;
				}
			}
		}
	}
}
