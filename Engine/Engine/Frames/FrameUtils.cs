using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Core.Input;
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



		/// <summary>
		/// Puts frame at center of parent frame
		/// </summary>
		/// <param name="frame"></param>
		public static void CenterFrame ( Frame frame )
		{
			if (frame.Parent==null) {
				throw new InvalidOperationException("CenterFrame could not be applied to top frame");
			}

			var parent = frame.Parent;

			frame.Anchor = FrameAnchor.None;

			frame.X	=	(parent.Width  - frame.Width )/2;
			frame.Y	=	(parent.Height - frame.Height)/2;
		}

	}
}

