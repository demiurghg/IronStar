using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System.Reflection;
using Fusion.Core.Mathematics;

namespace IronStar.Editor2.Controls {

	public partial class AEPropertyGrid : Frame {

		class AEBaseEditor : Frame {

			readonly public string Name;
			readonly protected AEPropertyGrid	grid;


			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public AEBaseEditor ( AEPropertyGrid grid, string name ) : base(grid.Frames)
			{ 
				this.BackColor	=	Color.Zero;
				this.grid		=	grid;
				this.Name		=	name;
			}
		}

	}
}
