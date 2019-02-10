using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics {
	public partial class Unwrapper {

		readonly Mesh			targetMesh;
		readonly int[]			axisMapping;
		readonly MeshVertex[]	vertices;
		readonly MeshTriangle[]	triangles;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="mesh"></param>
		/// <param name="projectionScale"></param>
		public Unwrapper ( Mesh mesh, float projectionScale )
		{
			this.targetMesh		=	mesh;

			vertices	=	mesh.Vertices.ToArray();
			triangles	=	mesh.Triangles.ToArray();
			axisMapping	=	new int[mesh.TriangleCount];
		}


	
		/// <summary>
		/// 
		/// </summary>
		public void Build ()
		{
			for ( int i=0; i<triangles.Length; i++ ) {

				axisMapping[i]	=	ProjectUVs( i );

			}

			targetMesh.Vertices	=	vertices.ToList();
		}



		/// <summary>
		/// Projects given triangle on dominant plane.
		/// </summary>
		/// <param name="triIndex"></param>
		/// <returns></returns>
		int ProjectUVs ( int triIndex )
		{
			var tri	=	triangles[ triIndex ];

			var p0	=	targetMesh.Vertices[ tri.Index0 ].Position;
			var p1	=	targetMesh.Vertices[ tri.Index1 ].Position;
			var p2	=	targetMesh.Vertices[ tri.Index2 ].Position;

			var v01	=	p1 - p0;
			var v02	=	p2 - p0;

			var n	=	Vector3.Cross( v01, v02 );

			var nn	=	Vector3.Normalize( n );

			var absX	=	Math.Abs( nn.X );
			var absY	=	Math.Abs( nn.Y );
			var absZ	=	Math.Abs( nn.Z );

			if ( absX >= absY && absX >= absZ ) 
			{
				vertices[ tri.Index0 ].TexCoord0.X	=	vertices[ tri.Index0 ].Position.Y;
				vertices[ tri.Index0 ].TexCoord0.Y	=	vertices[ tri.Index0 ].Position.Z;

				vertices[ tri.Index1 ].TexCoord0.X	=	vertices[ tri.Index1 ].Position.Y;
				vertices[ tri.Index1 ].TexCoord0.Y	=	vertices[ tri.Index1 ].Position.Z;

				vertices[ tri.Index2 ].TexCoord0.X	=	vertices[ tri.Index2 ].Position.Y;
				vertices[ tri.Index2 ].TexCoord0.Y	=	vertices[ tri.Index2 ].Position.Z;
				
				return 0;
			}
			
			if ( absY >= absX && absY >= absZ ) 
			{
				vertices[ tri.Index0 ].TexCoord0.X	=	vertices[ tri.Index0 ].Position.X;
				vertices[ tri.Index0 ].TexCoord0.Y	=	vertices[ tri.Index0 ].Position.Z;

				vertices[ tri.Index1 ].TexCoord0.X	=	vertices[ tri.Index1 ].Position.X;
				vertices[ tri.Index1 ].TexCoord0.Y	=	vertices[ tri.Index1 ].Position.Z;

				vertices[ tri.Index2 ].TexCoord0.X	=	vertices[ tri.Index2 ].Position.X;
				vertices[ tri.Index2 ].TexCoord0.Y	=	vertices[ tri.Index2 ].Position.Z;
				
				return 1;
			}
			
			if ( absZ >= absX && absZ >= absY ) 
			{
				vertices[ tri.Index0 ].TexCoord0.X	=	vertices[ tri.Index0 ].Position.X;
				vertices[ tri.Index0 ].TexCoord0.Y	=	vertices[ tri.Index0 ].Position.Y;

				vertices[ tri.Index1 ].TexCoord0.X	=	vertices[ tri.Index1 ].Position.X;
				vertices[ tri.Index1 ].TexCoord0.Y	=	vertices[ tri.Index1 ].Position.Y;

				vertices[ tri.Index2 ].TexCoord0.X	=	vertices[ tri.Index2 ].Position.X;
				vertices[ tri.Index2 ].TexCoord0.Y	=	vertices[ tri.Index2 ].Position.Y;
				
				return 2;
			}

			throw new InvalidOperationException("ProjectUVs failed");
			
		}
	}
}
