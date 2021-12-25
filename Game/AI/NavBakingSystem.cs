using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.SFX2;
using Native.NRecast;
using System.ComponentModel;
using Fusion.Core.Extensions;
using System.IO;
using Fusion.Core.Shell;
using Fusion.Build;

namespace IronStar.AI
{
	[ContentLoader(typeof(NavMesh))]
	class NavMeshLoader : ContentLoader
	{
		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return new NavMesh( stream.ReadAllBytes() );
		}
	}


	class NavBakingSystem : ISystem
	{
		readonly IGameState	gs;
		readonly string		navMeshPath;
		readonly Aspect		navGeometryAspect	=	new Aspect().Include<Transform,StaticCollisionComponent,RenderModel>();

		PolyMesh	polyMesh	=	null;


		public NavBakingSystem(IGameState gs, string mapName)
		{
			this.gs				=	gs;
			this.navMeshPath	=	Path.Combine("maps", "navmesh", mapName);
		}


		public class BakeNavMeshCommand : ICommand
		{
			readonly IGameState gs;

			public BakeNavMeshCommand( IGameState gs )
			{
				this.gs = gs;
			}

			public object Execute()
			{
				var navBaking = gs.GetService<NavBakingSystem>();

				navBaking.BuildNavmesh( navBaking.GetStaticGeometry() );

				return null;
			}
		}

		/*-----------------------------------------------------------------------------------------
		 *	System stuff
		-----------------------------------------------------------------------------------------*/

		public Aspect GetAspect()
		{
			return Aspect.Empty;
		}

		
		public void Add( IGameState gs, Entity e )
		{
		}

		
		public void Remove( IGameState gs, Entity e )
		{
		}

		
		public void Update( IGameState gs, GameTime gameTime )
		{
			if (polyMesh!=null)
			{
				DrawNavMesh( gs, polyMesh, gs.Game.RenderSystem.RenderWorld.Debug );
			}
		}

		/*-----------------------------------------------------------------------------------------
		 *	Navmesh generation
		-----------------------------------------------------------------------------------------*/

		class BuildData
		{
			public IGameState gs;
			public Vector3[] verts;
			public int[] inds;
			public bool[] walks;
		}

		void BuildNavmesh( BuildData bd )
		{
			Log.Message("Building navigation mesh...");

			var config = new Config();

			config.TileSize					=	0;
			config.BorderSize				=	0;
			config.CellSize					=	2.00f;
			config.CellHeight				=	2.00f;
			config.WalkableSlopeAngle		=	30.0f;
			config.WalkableHeight			=	6.0f;
			config.WalkableClimb			=	1.0f;
			config.WalkableRadius			=	1.0f;
			config.MaxEdgeLen				=	24;
			config.MaxSimplificationError	=	1.3f;
			config.MinRegionSize			=	16;
			config.MergeRegionSize			=	40;
			config.MaxVertsPerPoly			=	6;
			config.DetailSampleDist			=	12.0f;
			config.DetailSampleMaxError		=	1.0f;

			config.CellHeight		=	1.00f;
			config.CellSize			=	1.00f;
			//config.BBox				=	BoundingBox.FromPoints( verts );
			config.BBox			=	new BoundingBox( Vector3.One * (-600), Vector3.One*600 );
			config.MaxVertsPerPoly	=	6;

			if (bd.verts.Length<3 || bd.inds.Length<3)
			{
				Log.Warning("No geometry data. Skipped.");
				return;
			}

			try
			{
				byte[] polyData = null;
				var navData = NavMesh.Build( config, bd.verts, bd.inds, bd.walks, ref polyData );

				//	#TODO #AI #NAVMESH -- store polymesh on disk too.
				polyMesh	=	new PolyMesh( polyData );

				var build = gs.Game.GetService<Builder>();

				using ( var stream = build.CreateSourceFile( navMeshPath + ".bin" ) )
				{
					stream.Write( navData, 0, navData.Length );
				}
			} 
			catch ( Exception e )
			{
				Log.Error(e.ToString());
			}
		}


		BuildData GetStaticGeometry ()
		{
			var indices		=	new List<int>();
			var vertices	=	new List<Vector3>();
			var walkables	=	new List<bool>();

			foreach ( var entity in gs.QueryEntities(navGeometryAspect) )
			{
				var transform		=	entity.GetComponent<Transform>();
				var model			=	entity.GetComponent<RenderModel>();
				var staticCollision	=	entity.GetComponent<StaticCollisionComponent>();

				if (!string.IsNullOrWhiteSpace( model.scenePath ) && staticCollision.Collidable) 
				{
					var scene			=	gs.Content.Load( model.scenePath, Scene.Empty );
					var nodeCount		=	scene.Nodes.Count;
					var worldMatricies	=	new Matrix[nodeCount];

					scene.ComputeAbsoluteTransforms( worldMatricies );

					for ( int i=0; i<scene.Nodes.Count; i++) 
					{
						var worldMatrix	=	worldMatricies[i] * transform.TransformMatrix;
						var node		=	scene.Nodes[i];

						if (model.AcceptCollisionNode(node) && node.MeshIndex>=0)
						{
							var mesh	=	scene.Meshes[ node.MeshIndex ];

							indices.AddRange( mesh.GetIndices( vertices.Count ) );
							vertices.AddRange( mesh.Vertices.Select( v1 => Vector3.TransformCoordinate( v1.Position, worldMatrix ) ) );

							walkables.AddRange( Enumerable.Range( 0, mesh.TriangleCount ).Select( tri => staticCollision.Walkable ) );
						}
					}
				}
			}

			var bd		=	new BuildData();
			bd.gs		=	gs;
			bd.verts	=	vertices.ToArray();
			bd.inds		=	indices.ToArray();
			bd.walks	=	walkables.ToArray();

			return bd;
		}


		/*-----------------------------------------------------------------------------------------
		 *	Navmesh rendering
		-----------------------------------------------------------------------------------------*/

		public void DrawNavMesh( IGameState gs, PolyMesh mesh, DebugRender dr )
		{
			if (gs.Game.RenderSystem.SkipDebugRendering) 
			{
				return;
			}

			if (mesh!=null && gs.Game.GetService<AICore>().ShowNavigationMesh) 
			{
				var polyInds = new int[6];
				var polyAdjs = new int[6];

				var verts = mesh.GetPolyMeshVertices();

				foreach ( var p in verts ) 
				{
					dr.DrawWaypoint( p, 0.25f, Color.Black, 2 );
				}

				for ( int polyIndex = 0; polyIndex < mesh.GetNumPolys(); polyIndex++ ) 
				{
				
					int nverts =	mesh.GetPolygonVertexIndices( polyIndex, polyInds );
									mesh.GetPolygonAdjacencyIndices( polyIndex, polyAdjs );
					
					for ( int edgeInd = 0; edgeInd < nverts; edgeInd++ ) 
					{	
						var i0 = edgeInd;
						var i1 = (edgeInd+1 == nverts) ? 0 : edgeInd+1;
						var v0 = verts[ polyInds[ i0 ] ];	
						var v1 = verts[ polyInds[ i1 ] ];	

						int lineWidth = 1;
						var lineColor = Color.Black;

						if (polyAdjs[edgeInd]>=0) 
						{
							lineWidth = 4;
						}

						dr.DrawLine( v0, v1, lineColor, lineColor, lineWidth, lineWidth );
					}
				}
			}
		}
	}
}
