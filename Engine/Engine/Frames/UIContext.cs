using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Input;
using Fusion.Drivers.Graphics;
using System.Diagnostics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics;
using System.IO;
using Fusion.Core.Extensions;

namespace Fusion.Engine.Frames {

	/// <summary>
	/// Holds current root frame and current target frame.
	/// No events should passed behind the root frame.
	/// </summary>
	public class UIContext 
	{
		internal readonly Frame Root;
		internal Frame Target;

		internal UIContext( Frame modal, Frame target )
		{
			Root	=	modal;
			Target	=	target;
		}
	}
}
