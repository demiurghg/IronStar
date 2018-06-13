using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Input;
using Fusion.Engine.Graphics;
using Fusion.Engine.Common;
using Forms = System.Windows.Forms;


namespace Fusion.Engine.Frames {

	public partial class Frame {

		/// <summary>
		/// Constrains frame by its parent frame with given margin
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="margin"></param>
		public void ConstrainFrame( int margin )
		{
			if (Parent==null) {
				throw new InvalidOperationException("ConstrainFrame could not be applied to top frame");
			}

			var parent = Parent;

			X	=	Math.Max( margin, Math.Min( X, parent.Width  - Width  - margin ) );
			Y	=	Math.Max( margin, Math.Min( Y, parent.Height - Height - margin ) );
		}



		/// <summary>
		/// Puts frame at center of parent frame.
		/// This methods sets anchors to none.
		/// </summary>
		/// <param name="frame"></param>
		public void CenterFrame ()
		{
			if (Parent==null) {
				throw new InvalidOperationException("CenterFrame could not be applied to top frame");
			}

			var parent = Parent;

			this.Anchor = FrameAnchor.None;

			this.X	=	(parent.Width  - this.Width )/2;
			this.Y	=	(parent.Height - this.Height)/2;
		}

	}
}

