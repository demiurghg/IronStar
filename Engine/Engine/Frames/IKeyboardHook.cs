using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Input;

namespace Fusion.Engine.Frames 
{
	public interface IKeyboardHook 
	{
		bool KeyDown ( Keys key, bool shift, bool alt, bool ctrl );
		bool KeyUp ( Keys key, bool shift, bool alt, bool ctrl );
		bool TypeWrite ( Keys key, char keyChar, bool shift, bool alt, bool ctrl );
	}
}
