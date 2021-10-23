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

namespace Fusion 
{
	public static class Log 
	{
		public static event EventHandler MessageLogged;
		public static IEnumerable<Tuple<LogSeverity,string>> MemoryLog { get { return memoryLog.Logs; } }

		struct LogEntry
		{
			public DateTime		Time;
			public LogSeverity	Severity;
			public string		Message;
			public Exception	Exception;
		}

		static readonly ConcurrentQueue<LogEntry> logQueue;
		static readonly Thread logThread;

		static readonly ILogTarget[] targets;

		static readonly ColoredConsoleTarget	coloredConsole;
		static readonly MemoryLogTarget			memoryLog;

		public static LogSeverity LogLevel = LogSeverity.Debug;

		static Log()
		{
			coloredConsole			=	new ColoredConsoleTarget();
			memoryLog				=	new MemoryLogTarget();
			memoryLog.MessageLogged	+=	(s,e) => MessageLogged?.Invoke(s,e);

			targets					=	new ILogTarget[] { coloredConsole, memoryLog };

			logQueue				=	new ConcurrentQueue<LogEntry>();

			logThread				=	new Thread( LoggingLoop );
			logThread.IsBackground	=	true;
			logThread.Priority		=	ThreadPriority.BelowNormal;
			logThread.Name			=	"Log Thread";
			logThread.Start();
		}



		static void LoggingLoop()
		{
			LogEntry entry;

			while (true)
			{
				if (logQueue.TryDequeue( out entry ))
				{
					WriteMessage(entry.Severity,  entry.Message);
					WriteException(entry.Severity, entry.Exception);
				}
				else
				{
					Thread.Sleep(5);
				}
			}
		}


		static void WriteMessage( LogSeverity severity, string message )
		{
			if (message!=null)
			{
				var lines = message.Split(new[] { '\n','\r' }, StringSplitOptions.RemoveEmptyEntries);

				foreach ( var line in lines )
				{
					WriteLine( severity, line );
				}
			}
		}


		static void WriteException( LogSeverity severity, Exception e )
		{
			if (e!=null)
			{
				WriteMessage( severity, e.ToString() );
			}
		}


		static void WriteLine( LogSeverity severity, string text )
		{
			string prefix = "";

			if (severity==LogSeverity.Debug) prefix = "...";
			if (severity==LogSeverity.Trace) prefix = " - ";

			foreach ( var target in targets )
			{
				target.WriteLine( severity, prefix + text );
			}
		}


		static void PushMessage( LogSeverity severity, string text, Exception exception = null )
		{
			if (severity>=LogLevel)
			{
				logQueue.Enqueue(
					new LogEntry 
					{
						Message		=	text,
						Severity	=	severity,
						Exception	=	exception,
						Time		=	DateTime.Now
					}
				);
			}
		}


		public static void Trace ( string message )							{	PushMessage( LogSeverity.Trace	, message );						}
		public static void Trace ( string format, params object[] args )	{	PushMessage( LogSeverity.Trace	, string.Format(format, args) );	}
		public static void Trace ( Exception exception )					{	PushMessage( LogSeverity.Trace	, null, exception );				}

		public static void Debug ( string message )							{	PushMessage( LogSeverity.Debug	, message );						}
		public static void Debug ( string format, params object[] args )	{	PushMessage( LogSeverity.Debug	, string.Format(format, args) );	}
		public static void Debug ( Exception exception )					{	PushMessage( LogSeverity.Debug	, null, exception );				}

		public static void Message ( string message )						{	PushMessage( LogSeverity.Info	, message );						}
		public static void Message ( string format, params object[] args )	{	PushMessage( LogSeverity.Info	, string.Format(format, args) );	}
		public static void Message ( Exception exception )					{	PushMessage( LogSeverity.Info	, null, exception );				}

		public static void Warning ( string message )						{	PushMessage( LogSeverity.Warning, message );						}
		public static void Warning ( string format, params object[] args )	{	PushMessage( LogSeverity.Warning, string.Format(format, args) );	}
		public static void Warning ( Exception exception )					{	PushMessage( LogSeverity.Warning, null, exception );				}

		public static void Error ( string message )							{	PushMessage( LogSeverity.Error	, message );						}
		public static void Error ( string format, params object[] args )	{	PushMessage( LogSeverity.Error	, string.Format(format, args) );	}
		public static void Error ( Exception exception )					{	PushMessage( LogSeverity.Error	, null, exception );				}

		public static void Fatal ( string message )							{	PushMessage( LogSeverity.Fatal	, message );						}
		public static void Fatal ( string format, params object[] args )	{	PushMessage( LogSeverity.Fatal	, string.Format(format, args) );	}
		public static void Fatal ( Exception exception )					{	PushMessage( LogSeverity.Fatal	, null, exception );				}


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
