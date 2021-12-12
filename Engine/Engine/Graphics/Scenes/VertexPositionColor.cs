using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics.Scenes 
{
	public struct VertexPositionColor
	{
		[Vertex("POSITION")]	public Vector3	Position;
		[Vertex("COLOR")]		public Color	Color	;

		static public VertexInputElement[] Elements {
			get {
				return VertexInputElement.FromStructure( typeof(VertexColorTexture) );
			}
		}

		public VertexPositionColor(Vector3 position, Color color)
		{
			Position	=	position;
			Color		=	color;
		}

		public static VertexPositionColor Convert ( MeshVertex meshVertex )
		{
			VertexPositionColor v;
			v.Position	=	meshVertex.Position;
			v.Color		=	meshVertex.Color0;
			return v;
		}
	}
}
