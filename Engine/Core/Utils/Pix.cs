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
	public sealed class PixEvent : IDisposable {

		readonly CVEvent cvEvent;

		public PixEvent (string eventName) 
		{
			SafeNativeMethods._BeginEvent( 0, eventName );

			cvEvent	=	new CVEvent( eventName );
		}


		public void Dispose () 
		{
			SafeNativeMethods._EndEvent();

			cvEvent.Dispose();
		}


		public static void Marker ( string name )
		{
			SafeNativeMethods.SetMarker(0, name);
		}
	}
}
