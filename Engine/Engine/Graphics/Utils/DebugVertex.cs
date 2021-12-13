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
		[Vertex("NORMAL",0)] public Vector3 Normal;
		[Vertex("WIDTH", 0)] public float Width;
		[Vertex("COLOR", 0)] public Color Color;

		public DebugVertex( Vector3 pos, Color color )
		{
			Position	=	pos;
			Color		=	color;
			Width		=	1;
			Normal		=	Vector3.Zero;
		}

		public DebugVertex( Vector3 pos, Color color, float width )
		{
			Position	=	pos;
			Color		=	color;
			Width		=	width;
			Normal		=	Vector3.Zero;
		}

		public DebugVertex( Vector3 pos, Vector3 normal, Vector2 uv )
		{
			Position	=	pos;
			Color		=	Color.White;
			Width		=	1;
			Normal		=	normal;
		}

		public DebugVertex( Vector3 pos )
		{
			Position	=	pos;
			Color		=	Color.White;
			Width		=	1;
			Normal		=	Vector3.Zero;
		}
	}
}
