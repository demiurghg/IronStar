using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Input;
using Fusion.Engine.Graphics;
using Fusion.Engine.Common;
using Forms = System.Windows.Forms;


namespace Fusion.Engine.Frames {

	public static class FrameUtils {

		/// <summary>
		/// Constrains frame by its parent frame with given margin
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="margin"></param>
		public static void ConstrainFrame( Frame frame, int margin )
		{
			if (frame.Parent==null) {
				throw new InvalidOperationException("ConstrainFrame could not be applied to top frame");
			}

			var parent = frame.Parent;

			frame.X	=	Math.Max( margin, Math.Min( frame.X, parent.Width  - frame.Width  - margin ) );
			frame.Y	=	Math.Max( margin, Math.Min( frame.Y, parent.Height - frame.Height - margin ) );
		}

	}
}

