using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Content;
using Fusion.Core.Configuration;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Core;
using Fusion.Engine.Graphics.Lights;
using Fusion.Core.Shell;
using Fusion.Engine.Graphics.Bvh;
using System.Diagnostics;
using Fusion.Engine.Graphics.Scenes;

namespace Fusion.Engine.Graphics.GI
{
	public partial class RayTracer
	{
		public class RTData : DisposableBase
		{
			StructuredBuffer	sbPrimitives;
			StructuredBuffer	sbBvhTree;
			StructuredBuffer	sbVertexData;

			public RenderTarget2D	raytracedImage;

			public StructuredBuffer	Primitives { get { return sbPrimitives; } }
			public StructuredBuffer	BvhTree { get { return sbBvhTree; } }
			public StructuredBuffer	VertexData { get { return sbVertexData; } }


			public RTData( RenderSystem rs, Type vertexDataType, int primitiveCount, int nodesCount, int vertexCount )
			{
				sbPrimitives	=	new StructuredBuffer( rs.Device, typeof(TRIANGLE), primitiveCount,	StructuredBufferFlags.None );
				sbBvhTree		=	new StructuredBuffer( rs.Device, typeof(BVHNODE),  nodesCount,		StructuredBufferFlags.None );
				sbVertexData	=	new StructuredBuffer( rs.Device, vertexDataType,   vertexCount,		StructuredBufferFlags.None );
			}


			protected override void Dispose( bool disposing )
			{
				if (disposing)
				{
					SafeDispose( ref sbPrimitives	);
					SafeDispose( ref sbBvhTree		);
					SafeDispose( ref sbVertexData	);
				}

				base.Dispose( disposing );
			}
		}
	}
}
