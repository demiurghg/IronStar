using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.Scenes 
{
	public partial class Unwrapper 
	{

		/// <summary>
		/// Represents UV shell
		/// </summary>
		public class UVShell {

			readonly Mesh mesh;
			readonly UVTriangle[] tris;


			/// <summary>
			/// Creates instance of UV shell.
			/// </summary>
			public UVShell ( Mesh mesh, IEnumerable<UVTriangle> tris )
			{
				this.tris	=	tris.ToArray();
				this.mesh	=	mesh;
			}



			public void BuildTopology ()
			{
				var dict = new Dictionary<Tuple<Vector2,Vector2>, UVTriangle>();

				foreach ( var tri in tris ) {
					dict.Add( new Tuple<Vector2, Vector2>( tri.TexCoords[0], tri.TexCoords[1] ), tri );
					dict.Add( new Tuple<Vector2, Vector2>( tri.TexCoords[1], tri.TexCoords[2] ), tri );
					dict.Add( new Tuple<Vector2, Vector2>( tri.TexCoords[2], tri.TexCoords[0] ), tri );
				}

				foreach ( var tri in tris ) {

					var pair0	=	new Tuple<Vector2, Vector2>( tri.TexCoords[1], tri.TexCoords[0] );
					var pair1	=	new Tuple<Vector2, Vector2>( tri.TexCoords[2], tri.TexCoords[1] );
					var pair2	=	new Tuple<Vector2, Vector2>( tri.TexCoords[0], tri.TexCoords[2] );

					UVTriangle other;

					if ( dict.TryGetValue( pair0, out other ) ) {
						tri.Neighbours[0]	=	other;
					}

					if ( dict.TryGetValue( pair1, out other ) ) {
						tri.Neighbours[1]	=	other;
					}

					if ( dict.TryGetValue( pair2, out other ) ) {
						tri.Neighbours[2]	=	other;
					}
				}
			}
			
		}
	}
}
