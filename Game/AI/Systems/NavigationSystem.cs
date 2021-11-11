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

namespace IronStar.AI
{
	class NavigationSystem : ISystem
	{
		bool		navMeshDirty	=	false;
		NavMesh		navMesh			=	null;
		BackgroundWorker	worker;

		readonly Aspect	navGeometryAspect	=	new Aspect().Include<Transform,StaticCollisionComponent,RenderModel>();


		public NavigationSystem()
		{
			worker	=	new BackgroundWorker();
			worker.DoWork				+=	Worker_DoWork;
			worker.RunWorkerCompleted	+=	Worker_RunWorkerCompleted;
		}

		private void Worker_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
		{
			Log.Message("Navigation system : build completed");
			navMesh = (NavMesh)e.Result;
		}

		private void Worker_DoWork( object sender, DoWorkEventArgs e )
		{
			var buildData = (BuildData)e.Argument;
			e.Result = BuildNavmesh( buildData );
		}

		/*-----------------------------------------------------------------------------------------
		 *	System stuff
		-----------------------------------------------------------------------------------------*/

		public Aspect GetAspect()
		{
			return navGeometryAspect;
		}

		
		public void Add( IGameState gs, Entity e )
		{
			navMeshDirty = true;
		}

		
		public void Remove( IGameState gs, Entity e )
		{
			navMeshDirty = true;
		}

		
		public void Update( IGameState gs, GameTime gameTime )
		{
			if (navMeshDirty && !worker.IsBusy)
			{
				Log.Message("Navigation system : build started");
				worker.RunWorkerAsync( GetStaticGeometry(gs) );
				navMeshDirty = false;
			}

			DrawNavMesh( gs, navMesh, gs.Game.GetService<RenderSystem>().RenderWorld.Debug );
		}

		/*-----------------------------------------------------------------------------------------
		 *	Navmesh queries :
		-----------------------------------------------------------------------------------------*/

		public Vector3[] FindRoute( Vector3 startPoint, Vector3 endPoint )
		{
			return navMesh?.FindRoute( startPoint, endPoint );
		}

		public Vector3 GetReachablePointInRadius( Vector3 startPoint, float maxRadius )
		{
			if (navMesh==null) return startPoint;

			Vector3 result = startPoint;

			navMesh.GetRandomReachablePoint( startPoint, maxRadius, ref result );

			return result;
		}

		/*-----------------------------------------------------------------------------------------
		 *	Navmesh rendering
		-----------------------------------------------------------------------------------------*/

		public void DrawNavMesh( IGameState gs, NavMesh mesh, DebugRender dr )
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

			/*if (route!=null) {
				for (int i=0; i<route.Length-1; i++) {
					var v0 = route[i];
					var v1 = route[i+1];
					dr.DrawLine( v0, v1, Color.Red, Color.Red, 5, 5 );
				}
			} */
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

		NavMesh BuildNavmesh( BuildData bd )
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
				Log.Message("No geometry data. Skipped.");
				return null;
			}

			try
			{
				byte[] polyData = null;
				var navData = NavMesh.Build( config, bd.verts, bd.inds, bd.walks, ref polyData );
				return new NavMesh( navData, polyData );
			} 
			catch ( Exception e )
			{
				Log.Error(e.ToString());
				return null;
			}
		}


		BuildData GetStaticGeometry ( IGameState gs )
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
	}
}
