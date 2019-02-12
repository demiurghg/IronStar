using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics {
	public partial class Unwrapper {

		readonly MeshVertex[]	vertices;
		readonly MeshTriangle[]	triangles;

		readonly UVTriangle[]	uvTriangles;

		readonly List<UVShell>	uvShells = new List<UVShell>();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="mesh"></param>
		/// <param name="projectionScale"></param>
		public Unwrapper ( Mesh mesh, float projectionScale )
		{
			//	separate triangles
			mesh.SeparateTriangles();

			vertices	=	mesh.Vertices.ToArray();
			triangles	=	mesh.Triangles.ToArray();
			uvTriangles	=	new UVTriangle[ triangles.Length ];

			//	initial projection :			
			for ( int i=0; i<triangles.Length; i++ ) {
				uvTriangles[i]	=	ProjectUVs( mesh, i );
			}

			//	get initial shells :
			uvShells.Add( new UVShell( mesh, uvTriangles.Where( tri => tri.Projection==0 ) ) );
			uvShells.Add( new UVShell( mesh, uvTriangles.Where( tri => tri.Projection==1 ) ) );
			uvShells.Add( new UVShell( mesh, uvTriangles.Where( tri => tri.Projection==2 ) ) );

			foreach ( var shell in uvShells ) {
				shell.BuildTopology();
			}

			//	write vertices back
			for ( int i=0; i<triangles.Length; i++ ) {
				
				var tri	=	mesh.Triangles[i];
				
				var v0	=	mesh.Vertices[ tri.Index0 ]; 
				var v1	=	mesh.Vertices[ tri.Index1 ]; 
				var v2	=	mesh.Vertices[ tri.Index2 ]; 

				v0.TexCoord0	=	uvTriangles[i].TexCoords[0];
				v1.TexCoord0	=	uvTriangles[i].TexCoords[1];
				v2.TexCoord0	=	uvTriangles[i].TexCoords[2];

				mesh.Vertices[ tri.Index0 ] = v0;
				mesh.Vertices[ tri.Index1 ] = v1;
				mesh.Vertices[ tri.Index2 ] = v2;
			}

			//	merge vertices back :
			mesh.MergeVertices(0);
		}


	


		/// <summary>
		/// Projects given triangle on dominant plane.
		/// </summary>
		/// <param name="triIndex"></param>
		/// <returns></returns>
		UVTriangle ProjectUVs ( Mesh mesh, int triIndex )
		{
			var tri		=	triangles[ triIndex ];
			var nn		=	tri.ComputeNormal( mesh );

			var absX	=	Math.Abs( nn.X );
			var absY	=	Math.Abs( nn.Y );
			var absZ	=	Math.Abs( nn.Z );

			Vector2 tc0, tc1, tc2;

			if ( absX >= absY && absX >= absZ ) 
			{
				tc0.X	=	vertices[ tri.Index0 ].Position.Y;
				tc0.Y	=	vertices[ tri.Index0 ].Position.Z;

				tc1.X	=	vertices[ tri.Index1 ].Position.Y;
				tc1.Y	=	vertices[ tri.Index1 ].Position.Z;

				tc2.X	=	vertices[ tri.Index2 ].Position.Y;
				tc2.Y	=	vertices[ tri.Index2 ].Position.Z;

				return new UVTriangle( 0, triIndex, tc0, tc1, tc2 );
			}
			
			if ( absY >= absX && absY >= absZ ) 
			{
				tc0.X	=	vertices[ tri.Index0 ].Position.X;
				tc0.Y	=	vertices[ tri.Index0 ].Position.Z;

				tc1.X	=	vertices[ tri.Index1 ].Position.X;
				tc1.Y	=	vertices[ tri.Index1 ].Position.Z;

				tc2.X	=	vertices[ tri.Index2 ].Position.X;
				tc2.Y	=	vertices[ tri.Index2 ].Position.Z;

				return new UVTriangle( 1, triIndex, tc0, tc1, tc2 );
			}
			
			if ( absZ >= absX && absZ >= absY ) 
			{
				tc0.X	=	vertices[ tri.Index0 ].Position.X;
				tc0.Y	=	vertices[ tri.Index0 ].Position.Y;

				tc1.X	=	vertices[ tri.Index1 ].Position.X;
				tc1.Y	=	vertices[ tri.Index1 ].Position.Y;

				tc2.X	=	vertices[ tri.Index2 ].Position.X;
				tc2.Y	=	vertices[ tri.Index2 ].Position.Y;

				return new UVTriangle( 2, triIndex, tc0, tc1, tc2 );
			}

			throw new InvalidOperationException("ProjectUVs failed");
		}
	}
}
