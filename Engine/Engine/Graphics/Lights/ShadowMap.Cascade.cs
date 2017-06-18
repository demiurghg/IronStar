using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using Fusion.Build.Mapping;


namespace Fusion.Engine.Graphics {

	partial class ShadowMap {

		public class Cascade {

			public bool IsActive {
				get { return true; }
			}

			public int DetailLevel {
				get { return 0; }
			}
			
			public Matrix ViewMatrix { 
				get; set; 
			}	

			public Matrix ProjectionMatrix	{ 
				get; set; 
			}	

			public Matrix ViewProjectionMatrix {
				get {
					return ViewMatrix * ProjectionMatrix;
				}
			}

			public Rectangle ShadowRegion {
				get; set;
			}

			public Vector4 ShadowScaleOffset {
				get; set;
			}
		}

	}
}
