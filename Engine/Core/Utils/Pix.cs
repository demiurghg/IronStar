using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.ConcurrencyVisualizer.Instrumentation;

namespace Fusion {

	[System.Security.SuppressUnmanagedCodeSecurity]
	internal static class SafeNativeMethods {
		[DllImport("d3d9.dll",
			EntryPoint = "D3DPERF_BeginEvent",
			CharSet = CharSet.Unicode,
			CallingConvention = CallingConvention.Winapi)]
		internal static extern int _BeginEvent(uint col, string wszName);

		[DllImport("d3d9.dll",
			EntryPoint = "D3DPERF_EndEvent",
			CallingConvention = CallingConvention.Winapi)]
		internal static extern int _EndEvent();

		[DllImport("d3d9.dll",
			EntryPoint = "D3DPERF_SetMarker",
			CharSet = CharSet.Unicode,
			CallingConvention = CallingConvention.Winapi)]
		internal static extern void SetMarker(uint col, string wszName);
	}



	public sealed class PixEvent : IDisposable {

		static readonly MarkerSeries series = Markers.CreateMarkerSeries("Fusion");
		readonly Span span;

		public PixEvent (string eventName) {

			SafeNativeMethods._BeginEvent( 0, eventName );

			span	=	series.EnterSpan( eventName );

			/*StackTrace	st = new StackTrace();
			StackFrame sf = st.GetFrame(1);

			//string clsName = new string( sf.GetMethod().DeclaringType.Name.Where(ch=>char.IsUpper(ch)).ToArray() );
			string clsName = sf.GetMethod().DeclaringType.Name;
			SafeNativeMethods._BeginEvent( 0, clsName + "." + sf.GetMethod().Name + " - " + eventName );*/
		}

		public void Dispose () {
			SafeNativeMethods._EndEvent();
			span.Leave();
			//GC.SuppressFinalize(this);
		}


		public static void Marker ( string name )
		{
			SafeNativeMethods.SetMarker(0, name);
		}
	}
}
