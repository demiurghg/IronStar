using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System.Reflection;

namespace IronStar.Editor2.AttributeEditor {

	public partial class AEPropertyGrid : Frame {

		class BaseElement : Frame {

			public readonly BindingInfo BindingInfo;
			public readonly AEPropertyGrid PropertyGrid;

			public BaseElement ( AEPropertyGrid grid, BindingInfo bindingInfo ) : base(grid.Frames)
			{ 
				this.PropertyGrid	=	grid;
				this.BindingInfo	=	bindingInfo;
			}
		}

	}
}
