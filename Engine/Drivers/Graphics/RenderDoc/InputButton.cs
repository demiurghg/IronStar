using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Drivers.Graphics.RenderDoc
{
	public enum InputButton : uint 
	{
		// '0' - '9' matches ASCII values
		Key_0 = 0x30,
		Key_1 = 0x31,
		Key_2 = 0x32,
		Key_3 = 0x33,
		Key_4 = 0x34,
		Key_5 = 0x35,
		Key_6 = 0x36,
		Key_7 = 0x37,
		Key_8 = 0x38,
		Key_9 = 0x39,

		// 'A' - 'Z' matches ASCII values
		Key_A = 0x41,
		Key_B = 0x42,
		Key_C = 0x43,
		Key_D = 0x44,
		Key_E = 0x45,
		Key_F = 0x46,
		Key_G = 0x47,
		Key_H = 0x48,
		Key_I = 0x49,
		Key_J = 0x4A,
		Key_K = 0x4B,
		Key_L = 0x4C,
		Key_M = 0x4D,
		Key_N = 0x4E,
		Key_O = 0x4F,
		Key_P = 0x50,
		Key_Q = 0x51,
		Key_R = 0x52,
		Key_S = 0x53,
		Key_T = 0x54,
		Key_U = 0x55,
		Key_V = 0x56,
		Key_W = 0x57,
		Key_X = 0x58,
		Key_Y = 0x59,
		Key_Z = 0x5A,

		// leave the rest of the ASCII range free
		// in case we want to use it later
		Key_NonPrintable = 0x100,

		Key_Divide,
		Key_Multiply,
		Key_Subtract,
		Key_Plus,

		Key_F1,
		Key_F2,
		Key_F3,
		Key_F4,
		Key_F5,
		Key_F6,
		Key_F7,
		Key_F8,
		Key_F9,
		Key_F10,
		Key_F11,
		Key_F12,

		Key_Home,
		Key_End,
		Key_Insert,
		Key_Delete,
		Key_PageUp,
		Key_PageDn,

		Key_Backspace,
		Key_Tab,
		Key_PrtScrn,
		Key_Pause,

		Key_Max,
	}
}
