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
		[Vertex("POSITION")] public Vector4 Pos;
		[Vertex("COLOR", 0)] public Vector4 Color;

		/// <summary>
		/// Creates white vertex
		/// </summary>
		/// <param name="pos"></param>
		public DebugVertex( Vector3 pos )
		{
			Pos		=	new Vector4( pos, 1 );
			Color	=	Vector4.One;
		}
	}
}
