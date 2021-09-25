using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.IO.Pipes;
using CC = System.ConsoleColor;
using Fusion.Core.Mathematics;
using System.Threading;
using System.Collections.Concurrent;
using NLog;
using NLog.Targets;
using NLog.Conditions;
using NLog.Targets.Wrappers;
using NLog.LayoutRenderers;

namespace Fusion 
{
	public static class Log 
	{
		private static readonly Logger logger;
		private static readonly ConcurrentMemoryTarget memoryTarget;
		public static event EventHandler MessageLogged;


		static Log()
		{
			logger				=	LogManager.GetCurrentClassLogger();
			var config			=	new NLog.Config.LoggingConfiguration();

			LayoutRenderer.Register("trace-prefix", (logEvent) => logEvent.Level==LogLevel.Trace ? " - " : "" );
			LayoutRenderer.Register("debug-prefix", (logEvent) => logEvent.Level==LogLevel.Debug ? "..." : "" );


			memoryTarget		=	new ConcurrentMemoryTarget();
			memoryTarget.MaxLogsCount	=	1024;
			memoryTarget.MessageLogged	+=	(s,e) => MessageLogged?.Invoke(s,e);
			memoryTarget.Layout			=	"${debug-prefix}${trace-prefix}${message}";

			var logconsole		=	new NLog.Targets.ColoredConsoleTarget();

			logconsole.Layout	=	"${debug-prefix}${trace-prefix}${message}";
			logconsole.UseDefaultRowHighlightingRules = false;

			var condTrace		=	ConditionParser.ParseExpression("level == LogLevel.Trace");
			var condDebug		=	ConditionParser.ParseExpression("level == LogLevel.Debug");
			var condInfo		=	ConditionParser.ParseExpression("level == LogLevel.Info");
			var condWarning		=	ConditionParser.ParseExpression("level == LogLevel.Warning");
			var condError		=	ConditionParser.ParseExpression("level == LogLevel.Error");
			var condFatal		=	ConditionParser.ParseExpression("level == LogLevel.Fatal");
		
			logconsole.RowHighlightingRules.Add( new ConsoleRowHighlightingRule( condTrace	, ConsoleOutputColor.DarkGray,	ConsoleOutputColor.NoChange	) );
			logconsole.RowHighlightingRules.Add( new ConsoleRowHighlightingRule( condDebug	, ConsoleOutputColor.Gray,		ConsoleOutputColor.NoChange	) );
			logconsole.RowHighlightingRules.Add( new ConsoleRowHighlightingRule( condInfo	, ConsoleOutputColor.White,		ConsoleOutputColor.NoChange	) );
			logconsole.RowHighlightingRules.Add( new ConsoleRowHighlightingRule( condWarning, ConsoleOutputColor.Yellow,	ConsoleOutputColor.NoChange	) );
			logconsole.RowHighlightingRules.Add( new ConsoleRowHighlightingRule( condError	, ConsoleOutputColor.Red,		ConsoleOutputColor.NoChange	) );
			logconsole.RowHighlightingRules.Add( new ConsoleRowHighlightingRule( condFatal	, ConsoleOutputColor.White,		ConsoleOutputColor.DarkRed	) );

			/*logconsole.WordHighlightingRules.Add(
					new ConsoleWordHighlightingRule("Loading", 
					ConsoleOutputColor.NoChange, 
					ConsoleOutputColor.DarkBlue));
			logconsole.WordHighlightingRules.Add(
					new ConsoleWordHighlightingRule("Initialize",
						ConsoleOutputColor.NoChange,
						ConsoleOutputColor.DarkBlue)); //*/

			// Rules for mapping loggers to targets
			var asyncConsole	=	new AsyncTargetWrapper(logconsole);
			asyncConsole.BatchSize	=	64;

			config.AddRule(LogLevel.Trace, LogLevel.Fatal, logconsole);
			config.AddRule(LogLevel.Trace, LogLevel.Fatal, memoryTarget);

			//config.
			// Apply config
			LogManager.Configuration = config;
		}


		public static IEnumerable<Tuple<LogLevel,string>> MemoryLog { get { return memoryTarget.Logs; } }


		public static void Trace ( string message )							{	logger.Trace( message );		}
		public static void Trace ( string format, params object[] args )	{	logger.Trace( format, args );	}
		public static void Trace ( Exception exception )					{	logger.Trace( exception );		}

		public static void Debug ( string message )							{	logger.Debug( message );		}
		public static void Debug ( string format, params object[] args )	{	logger.Debug( format, args );	}
		public static void Debug ( Exception exception )					{	logger.Debug( exception );		}

		public static void Message ( string message )						{	logger.Info( message );			}
		public static void Message ( string format, params object[] args )	{	logger.Info( format, args );	}
		public static void Message ( Exception exception )					{	logger.Info( exception );		}

		public static void Warning ( string message )						{	logger.Warn( message );			}
		public static void Warning ( string format, params object[] args )	{	logger.Warn( format, args);		}
		public static void Warning ( Exception exception )					{	logger.Warn( exception );		}
		
		public static void Error ( string message )							{	logger.Error( message );		}
		public static void Error ( string format, params object[] args )	{	logger.Error( format, args );	}
		public static void Error ( Exception exception )					{	logger.Error( exception );		}

		public static void Fatal ( string message )							{	logger.Fatal( message );		}
		public static void Fatal ( string format, params object[] args )	{	logger.Fatal( format, args );	}
		public static void Fatal ( Exception exception )					{	logger.Fatal( exception );		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="array"></param>
		public static void Dump ( byte[] array )
		{
			Log.Trace( "---------------------------------------------------------------------");
			Log.Trace( string.Format("Dump: {0} bytes ({0:X8})", array.Length) );

			for (int i=0; i<MathUtil.IntDivRoundUp( array.Length, 16 ); i++) {

				int count	=	Math.Min(16, array.Length - i * 16);

				string hex	= "";
				string txt  = "";
				
				for (int j=0; j<count; j++) {
					
					var b  = array[i*16+j];
					var ch = (char)b;
					hex += b.ToString("x2");

					if (char.IsControl(ch)) {
						txt += ".";
					} else {
						txt += ch;
					}

					if (j==3||j==7||j==11) {
						hex += "  ";
					} else {
						hex += " ";
					}
				}

				Log.Trace( string.Format("{0,-51}| {1}", hex, txt) );
			}

			Log.Trace( "---------------------------------------------------------------------");
		}
	}
}
