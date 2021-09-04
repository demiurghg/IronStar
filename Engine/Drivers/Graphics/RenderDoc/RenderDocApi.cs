using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Drivers.Graphics.RenderDoc
{
	//
	//	https://renderdoc.org/docs/in_application_api.html
	//	https://stackoverflow.com/questions/20691786/c-sharp-pinvoke-with-c-struct-contains-function-pointer
	//
	// Sets an option that controls how RenderDoc behaves on capture.
	//
	// Returns 1 if the option and value are valid
	// Returns 0 if either is invalid and the option is unchanged
	public delegate int SetCaptureOptionU32(CaptureOption opt, uint val);
	public delegate int SetCaptureOptionF32(CaptureOption opt, float val);

	// Gets the current value of an option as a uint
	//
	// If the option is invalid, 0xffffffff is returned
	public delegate uint GetCaptureOptionU32(CaptureOption opt);

	// Gets the current value of an option as a float
	//
	// If the option is invalid, -FLT_MAX is returned
	public delegate float GetCaptureOptionF32(CaptureOption opt);

	// Sets which key or keys can be used to toggle focus between multiple windows
	//
	// If keys is NULL or num is 0, toggle keys will be disabled
	public delegate void SetFocusToggleKeys(InputButton[] keys, int num);

	// Sets which key or keys can be used to capture the next frame
	//
	// If keys is NULL or num is 0, captures keys will be disabled
	public delegate void SetCaptureKeys(InputButton[] keys, int num);

	// returns the overlay bits that have been set
	public delegate uint GetOverlayBits();
	// sets the overlay bits with an and & or mask
	public delegate void MaskOverlayBits(OverlayBits And, OverlayBits Or);

	// this function will attempt to remove RenderDoc's hooks in the application.
	//
	// Note: that this can only work correctly if done immediately after
	// the module is loaded, before any API work happens. RenderDoc will remove its
	// injected hooks and shut down. Behaviour is undefined if this is called
	// after any API functions have been called, and there is still no guarantee of
	// success.
	public delegate void RemoveHooks();

	// DEPRECATED: compatibility for code compiled against pre-1.4.1 headers.
	/*
	public delegate RemoveHooks Shutdown;
	*/

	// This function will unload RenderDoc's crash handler.
	//
	// If you use your own crash handler and don't want RenderDoc's handler to
	// intercede, you can call this function to unload it and any unhandled
	// exceptions will pass to the next handler.
	public delegate void UnloadCrashHandler();

	// Sets the capture file path template
	//
	// pathtemplate is a UTF-8 string that gives a template for how captures will be named
	// and where they will be saved.
	//
	// Any extension is stripped off the path, and captures are saved in the directory
	// specified, and named with the filename and the frame number appended. If the
	// directory does not exist it will be created, including any parent directories.
	//
	// If pathtemplate is NULL, the template will remain unchanged
	//
	// Example:
	//
	// SetCaptureFilePathTemplate("my_captures/example");
	//
	// Capture #1 -> my_captures/example_frame123.rdc
	// Capture #2 -> my_captures/example_frame456.rdc
	public delegate void SetCaptureFilePathTemplate(string pathtemplate);

	// returns the current capture path template, see SetCaptureFileTemplate above, as a UTF-8 string
	public delegate string GetCaptureFilePathTemplate();

	/*
	// DEPRECATED: compatibility for code compiled against pre-1.1.2 headers.
	public delegate SetCaptureFilePathTemplate SetLogFilePathTemplate;
	public delegate GetCaptureFilePathTemplate GetLogFilePathTemplate;
	*/

	// returns the number of captures that have been made
	public delegate uint GetNumCaptures();

	// This function returns the details of a capture, by index. New captures are added
	// to the end of the list.
	//
	// filename will be filled with the absolute path to the capture file, as a UTF-8 string
	// pathlength will be written with the length in bytes of the filename string
	// timestamp will be written with the time of the capture, in seconds since the Unix epoch
	//
	// Any of the parameters can be NULL and they'll be skipped.
	//
	// The function will return 1 if the capture index is valid, or 0 if the index is invalid
	// If the index is invalid, the values will be unchanged
	//
	// Note: when captures are deleted in the UI they will remain in this list, so the
	// capture path may not exist anymore.
	public delegate uint GetCapture(uint idx, string filename, ref uint pathlength, ref ulong timestamp);

	// Sets the comments associated with a capture file. These comments are displayed in the
	// UI program when opening.
	//
	// filePath should be a path to the capture file to add comments to. If set to NULL or ""
	// the most recent capture file created made will be used instead.
	// comments should be a NULL-terminated UTF-8 string to add as comments.
	//
	// Any existing comments will be overwritten.
	public delegate void SetCaptureFileComments(string filePath, string comments);

	// returns 1 if the RenderDoc UI is connected to this application, 0 otherwise
	public delegate uint IsTargetControlConnected();

	// DEPRECATED: compatibility for code compiled against pre-1.1.1 headers.
	// This was renamed to IsTargetControlConnected in API 1.1.1, the old public delegate is kept here for
	// backwards compatibility with old code, it is castable either way since it's ABI compatible
	// as the same function pointer type.
	/*
	public delegate IsTargetControlConnected IsRemoteAccessConnected;
	*/

	// This function will launch the Replay UI associated with the RenderDoc library injected
	// into the running application.
	//
	// if connectTargetControl is 1, the Replay UI will be launched with a command line parameter
	// to connect to this application
	// cmdline is the rest of the command line, as a UTF-8 string. E.g. a captures to open
	// if cmdline is NULL, the command line will be empty.
	//
	// returns the PID of the replay UI if successful, 0 if not successful.
	public delegate uint LaunchReplayUI(uint connectTargetControl, string cmdline);

	// RenderDoc can return a higher version than requested if it's backwards compatible,
	// this function returns the actual version returned. If a parameter is NULL, it will be
	// ignored and the others will be filled out.
	public delegate void GetAPIVersion(ref int major, ref int minor, ref int patch);

	// This sets the RenderDoc in-app overlay in the API/window pair as 'active' and it will
	// respond to keypresses. Neither parameter can be NULL
	public delegate void SetActiveWindow(IntPtr device,  IntPtr wndHandle);

	// capture the next frame on whichever window and API is currently considered active
	public delegate void TriggerCapture();

	// capture the next N frames on whichever window and API is currently considered active
	public delegate void TriggerMultiFrameCapture(uint numFrames);

	// When choosing either a device pointer or a window handle to capture, you can pass NULL.
	// Passing NULL specifies a 'wildcard' match against anything. This allows you to specify
	// any API rendering to a specific window, or a specific API instance rendering to any window,
	// or in the simplest case of one window and one API, you can just pass NULL for both.
	//
	// In either case, if there are two or more possible matching (device,window) pairs it
	// is undefined which one will be captured.
	//
	// Note: for headless rendering you can pass NULL for the window handle and either specify
	// a device pointer or leave it NULL as above.

	// Immediately starts capturing API calls on the specified device pointer and window handle.
	//
	// If there is no matching thing to capture (e.g. no supported API has been initialised),
	// this will do nothing.
	//
	// The results are undefined (including crashes) if two captures are started overlapping,
	// even on separate devices and/oror windows.
	public delegate void StartFrameCapture(IntPtr device, IntPtr wndHandle);

	// Returns whether or not a frame capture is currently ongoing anywhere.
	//
	// This will return 1 if a capture is ongoing, and 0 if there is no capture running
	public delegate uint IsFrameCapturing();

	// Ends capturing immediately.
	//
	// This will return 1 if the capture succeeded, and 0 if there was an error capturing.
	public delegate uint EndFrameCapture(IntPtr device, IntPtr wndHandle);

	// Ends capturing immediately and discard any data stored without saving to disk.
	//
	// This will return 1 if the capture was discarded, and 0 if there was an error or no capture
	// was in progress
	public delegate uint DiscardFrameCapture(IntPtr device, IntPtr wndHandle);
															   


	internal static class NativeMethods
	{
		[DllImport(@"renderdoc.dll", CallingConvention = CallingConvention.Cdecl)]
		public extern static int RENDERDOC_GetAPI( RenderDocApi.Version version, ref IntPtr api );
	}



	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RenderDocApi
	{
		public static RenderDocApi GetAPI(Version version)
		{
			var apiPtr	= IntPtr.Zero;
			var api		= new RenderDocApi();

			if (NativeMethods.RENDERDOC_GetAPI(version, ref apiPtr)!=1)
			{
				throw new Exception("Failed to load RenderDoc API");
			}

			api = (RenderDocApi)Marshal.PtrToStructure(apiPtr, typeof(RenderDocApi));

			return api;
		}

		public enum Version 
		{
			Version_1_0_0 = 10000,    // RENDERDOC_API_1_0_0 = 1 00 00
			Version_1_0_1 = 10001,    // RENDERDOC_API_1_0_1 = 1 00 01
			Version_1_0_2 = 10002,    // RENDERDOC_API_1_0_2 = 1 00 02
			Version_1_1_0 = 10100,    // RENDERDOC_API_1_1_0 = 1 01 00
			Version_1_1_1 = 10101,    // RENDERDOC_API_1_1_1 = 1 01 01
			Version_1_1_2 = 10102,    // RENDERDOC_API_1_1_2 = 1 01 02
			Version_1_2_0 = 10200,    // RENDERDOC_API_1_2_0 = 1 02 00
			Version_1_3_0 = 10300,    // RENDERDOC_API_1_3_0 = 1 03 00
			Version_1_4_0 = 10400,    // RENDERDOC_API_1_4_0 = 1 04 00
			Version_1_4_1 = 10401,    // RENDERDOC_API_1_4_1 = 1 04 01
		}

		[MarshalAs(UnmanagedType.FunctionPtr)]	public GetAPIVersion GetAPIVersion;

		[MarshalAs(UnmanagedType.FunctionPtr)]	public SetCaptureOptionU32 SetCaptureOptionU32;
		[MarshalAs(UnmanagedType.FunctionPtr)]	public SetCaptureOptionF32 SetCaptureOptionF32;

		[MarshalAs(UnmanagedType.FunctionPtr)]	public GetCaptureOptionU32 GetCaptureOptionU32;
		[MarshalAs(UnmanagedType.FunctionPtr)]	public GetCaptureOptionF32 GetCaptureOptionF32;

		[MarshalAs(UnmanagedType.FunctionPtr)]	public SetFocusToggleKeys SetFocusToggleKeys;
		[MarshalAs(UnmanagedType.FunctionPtr)]	public SetCaptureKeys SetCaptureKeys;

		[MarshalAs(UnmanagedType.FunctionPtr)]	public GetOverlayBits GetOverlayBits;
		[MarshalAs(UnmanagedType.FunctionPtr)]	public MaskOverlayBits MaskOverlayBits;

		// Shutdown was renamed to RemoveHooks in 1.4.1.
		[MarshalAs(UnmanagedType.FunctionPtr)]	public RemoveHooks RemoveHooks;

		[MarshalAs(UnmanagedType.FunctionPtr)]	public UnloadCrashHandler UnloadCrashHandler;

		// Get/SetLogFilePathTemplate was renamed to Get/SetCaptureFilePathTemplate in 1.1.2.
		// These unions allow old code to continue compiling without changes
		[MarshalAs(UnmanagedType.FunctionPtr)]	public SetCaptureFilePathTemplate SetCaptureFilePathTemplate;

		[MarshalAs(UnmanagedType.FunctionPtr)]	public GetCaptureFilePathTemplate GetCaptureFilePathTemplate;

		[MarshalAs(UnmanagedType.FunctionPtr)]	public GetNumCaptures GetNumCaptures;
		[MarshalAs(UnmanagedType.FunctionPtr)]	public GetCapture GetCapture;

		[MarshalAs(UnmanagedType.FunctionPtr)]	public TriggerCapture TriggerCapture;

		// IsRemoteAccessConnected was renamed to IsTargetControlConnected in 1.1.1.
		// This union allows old code to continue compiling without changes
		[MarshalAs(UnmanagedType.FunctionPtr)]	public IsTargetControlConnected IsTargetControlConnected;
		[MarshalAs(UnmanagedType.FunctionPtr)]	public LaunchReplayUI LaunchReplayUI;

		[MarshalAs(UnmanagedType.FunctionPtr)]	public SetActiveWindow SetActiveWindow;

		[MarshalAs(UnmanagedType.FunctionPtr)]	public StartFrameCapture StartFrameCapture;
		[MarshalAs(UnmanagedType.FunctionPtr)]	public IsFrameCapturing IsFrameCapturing;
		[MarshalAs(UnmanagedType.FunctionPtr)]	public EndFrameCapture EndFrameCapture;

		// new function in 1.1.0
		[MarshalAs(UnmanagedType.FunctionPtr)]	public TriggerMultiFrameCapture TriggerMultiFrameCapture;

		// new function in 1.2.0
		[MarshalAs(UnmanagedType.FunctionPtr)]	public SetCaptureFileComments SetCaptureFileComments;

		// new function in 1.4.0
		[MarshalAs(UnmanagedType.FunctionPtr)]	public DiscardFrameCapture DiscardFrameCapture;
	}
}
