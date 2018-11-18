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
using Newtonsoft.Json;
using Native.NRecast;

namespace IronStar.Mapping {
	public partial class Map {

		Vector3 pointStart	= new Vector3( 24.25f, -0.5f,  24.25f);
		Vector3 pointEnd	= new Vector3(-24.25f, -0.5f, -24.25f);

		NavigationMesh mesh;
		BuildConfig config;

		Vector3[] route;

		#region Get static geometry
		public void GetStaticGeometry ( ContentManager content, out Vector3[] verts, out int[] inds )
		{
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

			verts = vertices.ToArray();
			inds  = indices.ToArray();
		}
		#endregion
		

		public void BuildNavMesh ( ContentManager content )
		{
			Vector3[] verts;
			int[] inds;

			GetStaticGeometry( content, out verts, out inds );

			config = new BuildConfig();
			config.CellHeight		=	0.25f;
			config.CellSize			=	0.25f;
			config.BBox				=	BoundingBox.FromPoints( verts );
			//config.BBox			=	new BoundingBox( Vector3.One * (-4), Vector3.One*4 );
			config.MaxVertsPerPoly	=	3;

			mesh = new NavigationMesh( config, verts, inds );

			route = mesh.FindRoute( pointStart, pointEnd );
		}



		public void DrawNavMesh( DebugRender dr )
		{
			if (mesh!=null) {

				var polyInds = new int[6];
				var polyAdjs = new int[6];

				var verts = mesh.GetPolyMeshVertices();

				foreach ( var p in verts ) {
					dr.DrawWaypoint( p, 0.25f, Color.Black, 2 );
				}

				for ( int polyIndex = 0; polyIndex < mesh.GetNumPolys(); polyIndex++ ) {
				
					int nverts =	mesh.GetPolygonVertexIndices( polyIndex, polyInds );
									mesh.GetPolygonAdjacencyIndices( polyIndex, polyAdjs );
					
					for ( int edgeInd = 0; edgeInd < nverts; edgeInd++ ) {	
						
						var i0 = edgeInd;
						var i1 = (edgeInd+1 == nverts) ? 0 : edgeInd+1;
						var v0 = verts[ polyInds[ i0 ] ];	
						var v1 = verts[ polyInds[ i1 ] ];	

						int lineWidth = 1;
						var lineColor = Color.Black;

						if (polyAdjs[edgeInd]>=0) {
							lineWidth = 2;
						}

						dr.DrawLine( v0, v1, lineColor, lineColor, lineWidth, lineWidth );
					}
				}
			}

			if (route!=null) {
				for (int i=0; i<route.Length-1; i++) {

					var v0 = route[i];
					var v1 = route[i+1];
					dr.DrawLine( v0, v1, Color.Red, Color.Red, 5, 5 );

				}
			}
		}
	}
}
