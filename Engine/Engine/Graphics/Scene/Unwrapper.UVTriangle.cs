using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics {
	public partial class Unwrapper {

		/// <summary>
		/// Represents UV triangle
		/// </summary>
		public class UVTriangle {

			public readonly int Projection;
			public readonly int TriangleIndex;
			public readonly Vector2[] TexCoords = new Vector2[3];
			public readonly UVTriangle[] Neighbours = new UVTriangle[3] { null, null, null };

			/// <summary>
			/// Creates of UV triangle
			/// </summary>
			public UVTriangle ( int projection, int triIndex, Vector2 tc0, Vector2 tc1, Vector2 tc2 )
			{										
				Projection		=	projection;
				TriangleIndex	=	triIndex;
				TexCoords[0]	=	tc0;
				TexCoords[1]	=	tc1;
				TexCoords[2]	=	tc2;
			}


		}
	}
}
