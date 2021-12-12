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
	public struct VertexPositionNormalTexture
	{

		[Vertex("POSITION")]	public Vector3	Position;
		[Vertex("TEXCOORD")]	public Vector2	TexCoord;
		[Vertex("NORMAL")]		public Vector3	Normal;


		static public VertexInputElement[] Elements {
			get {
				return VertexInputElement.FromStructure( typeof(VertexColorTextureNormal) );
			}
		}

		public VertexPositionNormalTexture(Vector3 position, Vector3 normal, Vector2 texcoord)
		{
			Position	=	position;
			Normal		=	normal;
			TexCoord	=	texcoord;
		}

		public static VertexPositionNormalTexture Convert ( MeshVertex meshVertex )
		{
			VertexPositionNormalTexture v;
			v.Position	=	meshVertex.Position;
			v.TexCoord	=	meshVertex.TexCoord0;
			v.Normal	=	meshVertex.Normal;
			return v;
		}
	}
}
