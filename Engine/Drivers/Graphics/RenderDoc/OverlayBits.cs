using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Drivers.Graphics.RenderDoc
{
	[Flags]
	public enum OverlayBits : uint
	{
		// This single bit controls whether the overlay is enabled or disabled globally
		Enabled = 0x1,

		// Show the average framerate over several seconds as well as min/max
		FrameRate = 0x2,

		// Show the current frame number
		FrameNumber = 0x4,

		// Show a list of recent captures, and how many captures have been made
		CaptureList = 0x8,

		// Default values for the overlay mask
		Default = (Enabled | FrameRate | FrameNumber | CaptureList),

		// Enable all bits
		All = 0xFFFFFFFF,

		// Disable all bits
		None = 0,
	}
}
