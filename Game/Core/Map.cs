﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Core.Extensions;
using BEPUphysics.BroadPhaseEntries;

namespace IronStar.Core {
	public class Map {

		List<MeshInstance> instances;
		List<StaticMesh> collisionMeshes;
		List<SpawnInfo> spawnInfos;

		/// <summary>
		/// Gets list of mesh instances.
		/// </summary>
		public IEnumerable<MeshInstance> MeshInstance {
			get {
				return instances;
			}
		}


		/// <summary>
		/// Gets list of static collision meshes
		/// </summary>
		public IEnumerable<StaticMesh> StaticCollisionMeshes {
			get {
				return collisionMeshes;
			}
		}


		/// <summary>
		/// Gets list of spawn infos
		/// </summary>
		public IEnumerable<SpawnInfo> SpawnInfos {
			get {
				return spawnInfos;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		public class SpawnInfo {
			public SpawnInfo( string classname, Vector3 origin, Quaternion rotation ) 
			{
				Classname	=	classname;
				Origin		=	origin;
				Rotation	=	rotation;
			}

			public readonly string Classname;
			public readonly Vector3 Origin;
			public readonly Quaternion Rotation;
		}


		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="content"></param>
		/// <param name="scene"></param>
		public Map ( ContentManager content, Scene scene, bool createRendMeshes )
		{
			//	create entity list :
			instances		=   new List<MeshInstance>();
			collisionMeshes	=	new List<StaticMesh>();
			spawnInfos		=	new List<SpawnInfo>();

			//	compute absolute transforms :
			var transforms  = new Matrix[ scene.Nodes.Count ];
			scene.ComputeAbsoluteTransforms( transforms );


			//	iterate through the scene's nodes :
			for ( int i = 0; i<scene.Nodes.Count; i++ ) {

				var node	=   scene.Nodes[i];
				var world   =   transforms[i];
				var name	=   node.Name;
				var desc	=	ParseComment(node.Comment);
				var mesh	=   node.MeshIndex < 0 ? null : scene.Meshes[ node.MeshIndex ];

				if (desc.ContainsKey("classname")) {
					var classname	=	desc["classname"];
					var origin		=	world.TranslationVector;
					var rotation	=	Quaternion.RotationMatrix( world );
					var spawnInfo	=	new SpawnInfo( classname, origin, rotation );
					spawnInfos.Add( spawnInfo );
					continue;
				}

				
				if ( mesh!=null ) {
					
					var staticMesh = CreateStaticMesh( mesh, world );

					collisionMeshes.Add( staticMesh );

					if ( createRendMeshes ) {
						var mi      = new MeshInstance( content.Game.RenderSystem, scene, mesh );
						mi.World    = world;

						instances.Add( mi );
					}
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="mesh"></param>
		/// <param name="world"></param>
		/// <returns></returns>
		StaticMesh CreateStaticMesh ( Mesh mesh, Matrix world )
		{
			var indices     =   mesh.GetIndices();
			var vertices    =   mesh.Vertices
								.Select( v1 => Vector3.TransformCoordinate( v1.Position, world ) )
								.Select( v2 => MathConverter.Convert( v2 ) )
								.ToArray();

			var staticMesh = new StaticMesh( vertices, indices );
			staticMesh.Sidedness = BEPUutilities.TriangleSidedness.Clockwise;

			return staticMesh;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static Dictionary<string, string> ParseComment ( string text )
		{
			return 
				text
				.Split( new[] {'\n','\r'}, StringSplitOptions.RemoveEmptyEntries )
				.Select( line1 => line1.Trim() )
				.Where( line2 => !line2.StartsWith("#") )
				.Select( line3 => line3.Split(new[]{'='}, 2) )
				.Where( pair => pair.Length == 2 )
				.ToDictionary( pair2 => pair2[0].Trim(), pair3 => pair3[1].Trim() );
		}
	}
}

	