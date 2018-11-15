using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Reflection;
using Native.Fbx;
using IronStar.Entities;
using Fusion.Core.Content;
using System.IO;
using IronStar.Core;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using BEPUphysics.BroadPhaseEntries;
using Fusion.Core.Mathematics;
using Fusion;
using Native.Recast;
using Newtonsoft.Json;

namespace IronStar.Mapping {
	public partial class Map {

		[XmlIgnore]
		[JsonIgnore]
		public Native.Recast.RCConfig NavConfig { get; set; } = new RCConfig();

		[XmlIgnore]
		[JsonIgnore]
		public RecastMesh SourceNavigationMesh;

		[XmlIgnore]
		[JsonIgnore]
		public PolyMesh NavigationMesh;

		int[] navIndices;
		Vector3[] navVertices;



		/// <summary>
		/// 
		/// </summary>
		public void BuildNavigationMesh ( ContentManager content )
		{
			NavConfig.CellSize				= 0.3f;
            NavConfig.CellHeight			= 0.2f;
            NavConfig.WalkableSlopeAngle	= 45f;
            NavConfig.WalkableHeight		= (int)Math.Ceiling((2f / NavConfig.CellHeight));
            NavConfig.WalkableClimb			= (int)Math.Floor(0.9f / NavConfig.CellHeight);
            NavConfig.WalkableRadius		= (int)Math.Ceiling(0.6f / NavConfig.CellSize);
            NavConfig.MaxEdgeLen			= (int)(12f / NavConfig.CellSize);
            NavConfig.MaxSimplificationError = 1.3f;
            NavConfig.MinRegionArea			= (int)8 * 8;
            NavConfig.MergeRegionArea		= (int)20 * 20;
            NavConfig.DetailSampleDist		= 6f;
            NavConfig.DetailSampleMaxError	= 1f;
            NavConfig.MaxVertsPerPoly		= 6;		


			var indices = new List<int>();
			var vertices = new List<Vector3>();

			var staticModels = Nodes
					.Select( n1 => n1 as MapModel )
					.Where( n2 => n2 != null )
					.ToArray();


			foreach ( var mapNode in staticModels ) {

				if (!string.IsNullOrWhiteSpace( mapNode.ScenePath )) {

					var scene = content.Load<Scene>( mapNode.ScenePath );

					var nodeCount = scene.Nodes.Count;
					
					var worldMatricies = new Matrix[nodeCount];

					scene.ComputeAbsoluteTransforms( worldMatricies );

					for ( int i=0; i<scene.Nodes.Count; i++) {

						var worldMatrix = worldMatricies[i] * mapNode.WorldMatrix;

						var node = scene.Nodes[i];

						if (node.MeshIndex<0) {
							continue;
						}

						var mesh		=	scene.Meshes[ node.MeshIndex ];

						indices.AddRange( mesh.GetIndices( vertices.Count ) );

						vertices.AddRange( mesh.Vertices.Select( v1 => Vector3.TransformCoordinate( v1.Position, worldMatrix ) ) );
					}
				}
			}


            var rcmesh = new RecastMesh();
            rcmesh.Indices = indices.ToArray();
            rcmesh.Vertices = vertices.ToArray();
            var context = new BuildContext(false);

			SourceNavigationMesh = rcmesh;
 
            NavigationMesh = RecastBuilder.BuildNavigationMesh( rcmesh, NavConfig, context, true );


			var cs = NavConfig.CellSize;
			var ch = NavConfig.CellHeight;
			var t = NavigationMesh;

            navVertices = new Vector3[t.VerticesCount];
			navIndices = new int[t.PolysCount*3];

			//Vector3 origin = NavConfig.BMin;

			/*for (int i = 0; i < t.PolysCount; i++) {

				for (int j = 0; j < 3; j++) {
					var index = t.Polys[i * 6 + j];
					navIndices[i*3+j] = index;

					ushort a = t.Verts[index * 3];
					ushort b = t.Verts[index * 3 + 1];
					ushort c = t.Verts[index * 3 + 2];
					Vector3 vertex = origin + new Vector3(a * cs, b * ch, c * cs);
					navVertices[index] = vertex;
				}
			} */
		}



		public void DrawNavigationMeshDebug ( DebugRender dr )
		{
			if (NavigationMesh==null) {
				return;
			}

			var color = Color.Yellow;

			var cs = NavConfig.CellSize;
			var ch = NavConfig.CellHeight;
			var navMesh = NavigationMesh;

			Vector3 origin = NavConfig.BMin;

			var polyVerts = new Vector3[6];
			var polyVertsCount = 0;

			for (int i = 0; i < navMesh.PolysCount; i++) {

				polyVertsCount = 0;

				for (int j = 0; j < 6; j++) {
					
					var index = navMesh.Polys[i * 6 + j];

					if (index==65535) {
						continue;
					}

					ushort a = navMesh.Verts[index * 3];
					ushort b = navMesh.Verts[index * 3 + 1];
					ushort c = navMesh.Verts[index * 3 + 2];
					Vector3 vertex = origin + new Vector3(a * cs, b * ch, c * cs);
					polyVerts[polyVertsCount] = vertex;

					polyVertsCount++;
				}


				if (polyVertsCount>0) {
					for (int k=0; k<polyVertsCount; k++) {
						var k0 = k;
						var k1 = (k + 1) % polyVertsCount;
						var p0 = polyVerts[k0];
						var p1 = polyVerts[k1];
					
						dr.DrawLine( p0, p1, color, color, 2,2 );
					}
				}
			}






			/*if (SourceNavigationMesh!=null) {

				var srcNavMesh = SourceNavigationMesh;
				foreach ( var p in srcNavMesh.Vertices ) {
					//dr.DrawPoint( p, 0.1f, Color.Yellow );
				}

			}


			if (navIndices==null) {
				return;
			}
			

			foreach ( var p in navVertices ) {
				dr.DrawPoint( p, 0.1f, Color.Yellow );
			}

			var color = Color.Yellow;

			for (int i=0; i<navIndices.Length/3; i++) {

				var p0 = navVertices[ navIndices[i*3+0] ];
				var p1 = navVertices[ navIndices[i*3+1] ];
				var p2 = navVertices[ navIndices[i*3+2] ];

				dr.DrawLine( p0, p1, color, color, 2,2 );
				dr.DrawLine( p1, p2, color, color, 2,2 );
				dr.DrawLine( p2, p0, color, color, 2,2 );
			}*/
		}
		
	}
}
