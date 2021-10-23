using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion
{
	interface ILogTarget
	{
		void WriteLine( LogSeverity severity, string text );
	}
}
