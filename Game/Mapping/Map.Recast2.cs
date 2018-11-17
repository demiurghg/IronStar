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
using Native.Recast;

namespace IronStar.Mapping {
	public partial class Map {

		Vector3 pointStart	= new Vector3( 24.25f, -0.5f,  24.25f);
		Vector3 pointEnd	= new Vector3(-24.25f, -0.5f, -24.25f);

		RCMesh mesh;
		RCBuildConfig config;

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

			config = new RCBuildConfig();
			config.CellHeight	=	0.25f;
			config.CellSize		=	0.25f;
			config.BBox			=	BoundingBox.FromPoints( verts );

			mesh = new RCMesh( config, verts, inds );

		}



		public void DrawNavMesh( DebugRender dr )
		{
			if (mesh!=null) {

				var verts = mesh.GetPolyMeshVertices();

				foreach ( var p in verts ) {
					dr.DrawWaypoint( p, 0.25f, Color.Black, 2 );
				}

			}
		}
	}
}
