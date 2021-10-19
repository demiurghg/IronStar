using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.ConcurrencyVisualizer.Instrumentation;
using System.Threading;

namespace Fusion {

	/// <summary>
	/// In case of "Strong name validation failed."
	/// https://stackoverflow.com/questions/403731/strong-name-validation-failed
	/// 
	/// Open the command prompt as administrator and enter following commands:
	///
	///	reg DELETE "HKLM\Software\Microsoft\StrongName\Verification" /f
	///	reg ADD "HKLM\Software\Microsoft\StrongName\Verification\*,*" /f
	///	reg DELETE "HKLM\Software\Wow6432Node\Microsoft\StrongName\Verification" /f
	///	reg ADD "HKLM\Software\Wow6432Node\Microsoft\StrongName\Verification\*,*" /f
	/// 
	/// </summary>
	public sealed class CVEvent : IDisposable 
	{
		const int MaxSeries = 16;

		[ThreadStatic] 
		static int calls = 0;

		[ThreadStatic] 
		static MarkerSeries[] series;

		readonly Span span;


		static MarkerSeries[] CreateSeries()
		{
			int threadId	=	Thread.CurrentThread.ManagedThreadId;

			return Enumerable
				.Range(0, MaxSeries)
				.Select( index => Markers.CreateMarkerSeries( "Th:" + threadId.ToString() + "[" + index.ToString() + "]" ) )
				.ToArray();
		}


	#if true
		public CVEvent (string eventName) 
		{
			if (series==null) 
			{
				series = CreateSeries();
			}

			span	=	series[calls].EnterSpan( eventName );
			calls++;

			if (calls>=MaxSeries) 
			{
				throw new IndexOutOfRangeException("Too much nested CVEvents");
			}
		}

		public void Dispose () 
		{
			span.Leave();
			calls--;
		}
	#else
		public CVEvent (string eventName) 
		{
		}

		public void Dispose () 
		{
		}
	#endif
	}
}
