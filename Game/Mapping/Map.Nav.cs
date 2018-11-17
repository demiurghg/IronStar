﻿using System;
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
using NRecast.Recast;
using NRecast.Detour;
using IronStar.AI;

namespace IronStar.Mapping {
	public partial class Map {

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

		NavigationGraph navGraph;
		

		public void BuildNavMesh ( ContentManager content )
		{
			Vector3[] verts;
			int[] inds;

			GetStaticGeometry( content, out verts, out inds );

			var settings = new NavigationSettings();
			settings.WalkableAngle	=	3.14f / 4;
			settings.VoxelStep		=	1.0f / 2.0f;
			settings.WalkableRadius	=	0.6f;

			navGraph = new NavigationGraph( settings, verts, inds );
		}



		public void DrawNavMesh( DebugRender dr )
		{
			navGraph?.DrawGraphDebug( dr );
		}
	}
}