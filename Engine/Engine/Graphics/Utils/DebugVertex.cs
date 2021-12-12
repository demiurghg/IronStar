using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using System.Runtime.InteropServices;
using Fusion.Engine.Graphics.Ubershaders;

namespace Fusion.Engine.Graphics 
{
	public struct DebugVertex 
	{
		[Vertex("POSITION")] public Vector3 Position;
		[Vertex("WIDTH", 0)] public float Width;
		[Vertex("COLOR", 0)] public Color Color;

		public DebugVertex( Vector3 pos, Color color )
		{
			Position	=	pos;
			Color		=	color;
			Width		=	1;
		}

		public DebugVertex( Vector3 pos, Color color, float width )
		{
			Position	=	pos;
			Color		=	color;
			Width		=	width;
		}

		public DebugVertex( Vector3 pos )
		{
			Position	=	pos;
			Color		=	Color.White;
			Width		=	1;
		}
	}
}
