using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion
{
	class ColoredConsoleTarget : ILogTarget
	{
		public void WriteLine( LogSeverity severity, string text )
		{
			switch ( severity )
			{
				case LogSeverity.Fatal:
					Console.BackgroundColor	=	ConsoleColor.Red;
					Console.ForegroundColor	=	ConsoleColor.White;
					break;
				case LogSeverity.Error:
					Console.BackgroundColor	=	ConsoleColor.Black;
					Console.ForegroundColor	=	ConsoleColor.Red;
					break;
				case LogSeverity.Warning:
					Console.BackgroundColor	=	ConsoleColor.Black;
					Console.ForegroundColor	=	ConsoleColor.Yellow;
					break;
				case LogSeverity.Info:
					Console.BackgroundColor	=	ConsoleColor.Black;
					Console.ForegroundColor	=	ConsoleColor.White;
					break;
				case LogSeverity.Debug:
					Console.BackgroundColor	=	ConsoleColor.Black;
					Console.ForegroundColor	=	ConsoleColor.Gray;
					break;
				case LogSeverity.Trace:
					Console.BackgroundColor	=	ConsoleColor.Black;
					Console.ForegroundColor	=	ConsoleColor.DarkGray;
					break;
				default:
					break;
			}

			if (text.Length>=Console.WindowWidth-1)
			{
				text = text.Substring(0, Console.WindowWidth-1);
			}		
			
			Console.WriteLine( text );
		}
	}
}
