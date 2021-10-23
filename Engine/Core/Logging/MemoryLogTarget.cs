using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion
{
	class MemoryLogTarget : ILogTarget
	{
		const int MaxLines = 1024;
							 
		public event EventHandler MessageLogged;

		readonly List<Tuple<LogSeverity,string>> logData;
		readonly object lockObj = new object();

		public MemoryLogTarget()
		{
			logData	=	new List<Tuple<LogSeverity, string>>(MaxLines+1);
		}
		
		public void WriteLine( LogSeverity severity, string text )
		{
			lock (lockObj)
			{
				logData.Add( Tuple.Create( severity, text ) );

				if (logData.Count>=MaxLines)
				{
					logData.RemoveAt(0);
				}

				MessageLogged?.Invoke(this, EventArgs.Empty);
			}
		}

		public Tuple<LogSeverity,string>[] Logs 
		{ 
			get { 
				lock (lockObj) 
				{ 
					return logData.ToArray();
				} 
			}
		}
	}
}
