using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Drivers.Graphics.RenderDoc
{
	public static class RenderDoc
	{
		static bool initialized = false;
		static RenderDocApi api = new RenderDocApi();

		public static void Initialize()
		{
			try 
			{
				api = RenderDocApi.GetAPI(RenderDocApi.Version.Version_1_4_0);
			} 
			catch ( DllNotFoundException dllnfe )
			{
				Log.Message("RenderDoc DLL is not found: ", dllnfe.Message);
				return;
			}

			int major = 0;
			int minor = 0;
			int patch = 0;

			api.GetAPIVersion( ref major, ref minor, ref patch );
			Log.Message("RenderDoc {0}.{1}.{2} loaded", major, minor, patch);

			initialized = true;

			api.SetCaptureKeys(new[] {InputButton.Key_PrtScrn}, 1);
		}


		public static void TriggerCapture()
		{
			if (initialized)
			{
				api.TriggerCapture();
			}
		}
		

		public static void StartFrameCapture()
		{
			if (initialized)
			{
				api.StartFrameCapture(IntPtr.Zero, IntPtr.Zero);
			}
		}
		

		public static void EndFrameCapture()
		{
			if (initialized)
			{
				api.EndFrameCapture(IntPtr.Zero, IntPtr.Zero);
			}
		}
		
	}
}
