﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Content;
using IronStar.SFX;
using Fusion.Engine.Graphics;
using Fusion.Core;
using IronStar.Physics;
using IronStar.Entities;
using IronStar.Views;

namespace IronStar.Core {

	/// <summary>
	/// World represents entire game state.
	/// </summary>
	public partial class GameWorld : DisposableBase {

		public readonly Guid UserGuid;

		public AtomCollection Atoms { 
			get; set;
		}

		SnapshotWriter snapshotWriter = new SnapshotWriter();
		SnapshotReader snapshotReader = new SnapshotReader();

		public readonly Game Game;
		public readonly ContentManager Content;

		public delegate void EntityEventHandler ( object sender, EntityEventArgs e );

		List<uint> entityToKill = new List<uint>();

		public EntityCollection entities;
		uint idCounter = 1;

		public event EntityEventHandler EntitySpawned;
		public event EntityEventHandler EntityKilled;

		List<FXEvent> fxEvents = new List<FXEvent>();

		SFX.FXPlayback		fxPlayback = null;
		SFX.ModelManager	modelManager = null;
		PhysicsManager		physics	= null;

		public SFX.ModelManager	ModelManager { get { return modelManager; } }
		public SFX.FXPlayback	FXPlayback   { get { return fxPlayback; } }
		public PhysicsManager	Physics		{ get { return physics; } }

		readonly PlayerState playerState = new PlayerState();
		public PlayerState PlayerState { get { return playerState; } }


		public struct Environment {
			public Vector3 SunDirection;
			public float Turbidity;
			public float FogDensity;
			public float Gravity;
			public float SunIntensity;
		}

		public Environment environment;


		public readonly bool IsPresentationEnabled;

        bool debugGridOff = false;
        bool debugGridOn = false;
        Fusion.Engine.Input.Keys GridKey = Fusion.Engine.Input.Keys.M;

        List<GridVertex> vertices;
        List<GridEdge> edges;

        int width = 48 / 4;
        int height = 24 / 4;
        int large = 48 / 4;

        int offsetZ = -20;
        int offsetY = -1;
        int offsetX = -20;

        GridVertex[,,] gridArray;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="game"></param>
        public GameWorld( Game game, bool enablePresentation, Guid userGuid )
		{
			IsPresentationEnabled	=	enablePresentation;

			this.Game	=	game;

			this.UserGuid	=	userGuid;

			Content		=	new ContentManager( Game );
			entities	=	new EntityCollection();

			physics		=	new PhysicsManager( this, 16 );

			if (enablePresentation) {

				var rw = Game.RenderSystem.RenderWorld;

				rw.VirtualTexture = Content.Load<VirtualTexture>("*megatexture");
				fxPlayback		=	new SFX.FXPlayback( this );
				modelManager	=	new SFX.ModelManager( this );

				rw.LightSet.SpotAtlas		=	Content.Load<TextureAtlas>(@"spots\spots");
				rw.LightSet.DecalAtlas		=	Content.Load<TextureAtlas>(@"decals\decals");
			}

            
            gridArray = new GridVertex[large, height, width];
            GridUpdate();
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {

				if (IsPresentationEnabled) {
					Game.RenderSystem.RenderWorld.ClearWorld();

					SafeDispose( ref fxPlayback );
					SafeDispose( ref modelManager );
				}
			}

			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="frmt"></param>
		/// <param name="args"></param>
		protected void LogTrace ( string frmt, params object[] args )
		{
			var s = string.Format( frmt, args );
			Log.Verbose("world: " + s);
		}



		/// <summary>
		/// Simulates world.
		/// </summary>
		/// <param name="gameTime"></param>
		public virtual void SimulateWorld ( float elapsedTime )
		{
			UpdatePlayers( elapsedTime );

			physics.Update( elapsedTime );
				
			//
			//	Control entities :
			//
			ForEachEntity( e => e.ForeachController( c => c.Update( elapsedTime ) ) );

			//
			//	Kill entities :
			//
			CommitKilledEntities();
            
            DrawDebugGrid();

        }



		/// <summary>
		/// Updates visual and audial stuff
		/// </summary>
		/// <param name="gameTime"></param>
		public void PresentWorld ( float deltaTime, float lerpFactor, GameCamera gameCamera )
		{
			var dr = Game.RenderSystem.RenderWorld.Debug;
			var rw = Game.RenderSystem.RenderWorld;

			var visibleEntities = entities.Select( pair => pair.Value ).ToArray();

			//
			//	draw all entities :
			//
			foreach ( var entity in visibleEntities ) {
				entity.UpdateRenderState( fxPlayback, modelManager, gameCamera );
			}

			//
			//	update view models :
			//
			var playerEntity = GetPlayerEntity( this.UserGuid );
			playerState.UpdateRenderState( playerEntity, FXPlayback, modelManager );


			//
			//	updare effects :
			//	
			foreach ( var fxe in fxEvents ) {
				fxPlayback.RunFX( fxe, false );
			}
			fxEvents.Clear();


			fxPlayback.Update( deltaTime, lerpFactor );
			modelManager.Update( deltaTime, lerpFactor, gameCamera );

			//
			//	update environment :
			//
			rw.HdrSettings.BloomAmount			= 0.1f;
			rw.HdrSettings.DirtAmount			= 0.0f;
			rw.HdrSettings.KeyValue				= 0.18f;

			rw.SkySettings.SunPosition			=	environment.SunDirection;
			rw.SkySettings.SunLightIntensity	=	environment.SunIntensity;
			rw.SkySettings.SkyTurbidity			=	environment.Turbidity;
			rw.SkySettings.SkyIntensity			=	0.5f;

			rw.FogSettings.Density				=	environment.FogDensity;

			rw.LightSet.DirectLight.Direction	=	rw.SkySettings.SunLightDirection;
			rw.LightSet.DirectLight.Intensity	=	rw.SkySettings.SunLightColor;

			//rw.LightSet.AmbientLevel	=	rw.SkySettings.AmbientLevel;

		}


		/*-----------------------------------------------------------------------------------------
		 *	Entity creation
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="factory"></param>
		/// <param name="classID"></param>
		/// <param name="parentId"></param>
		/// <param name="origin"></param>
		/// <param name="orient"></param>
		/// <returns></returns>
		public Entity Spawn( EntityFactory factory, short classID, uint parentId, Vector3 origin, Quaternion orient, string targetName )
		{
			//	get ID :
			uint id = idCounter;

			idCounter++;

			if ( idCounter==0 ) {
				//	this actually will never happen, about 103 day of intense playing.
				throw new InvalidOperationException( "Too much entities were spawned" );
			}

			//
			//	Create instance.
			//	If creation failed later, entity become dummy.
			//
			var entity = new Entity( id, classID, parentId, origin, orient, targetName );
			entities.Add( id, entity );

			entity.Controller = factory?.Spawn( entity, this );

			EntitySpawned?.Invoke( this, new EntityEventArgs( entity ) );

			return entity;
		}



		/// <summary>
		/// When called on client-side returns null.
		/// </summary>
		/// <param name="prefab"></param>
		/// <param name="parent"></param>
		/// <param name="origin"></param>
		/// <param name="angles"></param>
		/// <returns></returns>
		public Entity Spawn ( string classname, uint parentId, Vector3 origin, Quaternion orient )
		{
			var classID	=	Atoms[classname];
			var factory	=	Content.Load(@"entities\" + classname, (EntityFactory)null );

			return Spawn( factory, classID, parentId, origin, orient, null );
		}



		/// <summary>
		/// Spawns entity with specified classname, parent ID and matrix.
		/// </summary>
		/// <param name="prefab"></param>
		/// <param name="parentId"></param>
		/// <param name="transform"></param>
		/// <returns></returns>
		public Entity Spawn( string classname, uint parentId, Matrix transform )
		{
			var p	=	transform.TranslationVector;
			var q	=	Quaternion.RotationMatrix( transform );

			return Spawn( classname, parentId, p, q );
		}



		/// <summary>
		/// Spawns entity with specified classname, parent ID and yaw angle.
		/// </summary>
		/// <param name="prefab"></param>
		/// <param name="parentId"></param>
		/// <param name="origin"></param>
		/// <param name="yaw"></param>
		/// <returns></returns>
		public Entity Spawn( string classname, uint parentId, Vector3 origin, float yaw )
		{
			return Spawn( classname, parentId, origin, Quaternion.RotationYawPitchRoll( yaw,0,0 ) );
		}

		
		/*-----------------------------------------------------------------------------------------
		 *	FX creation
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxType"></param>
		/// <param name="position"></param>
		/// <param name="target"></param>
		/// <param name="orient"></param>
		public void SpawnFX ( string fxName, uint parentID, Vector3 origin, Vector3 velocity, Quaternion rotation )
		{
			//LogTrace("fx : {0}", fxName);
			var fxID = Atoms[ fxName ];

			if (fxID<0) {
				Log.Warning("SpawnFX: bad atom {0}", fxName);
			}

			fxEvents.Add( new FXEvent(fxID, parentID, origin, velocity, rotation ) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxType"></param>
		/// <param name="position"></param>
		/// <param name="target"></param>
		/// <param name="orient"></param>
		public void SpawnFX ( string fxName, uint parentID, Vector3 origin, Vector3 velocity, Vector3 forward )
		{
			forward	=	Vector3.Normalize( forward );
			var rt	=	Vector3.Cross( forward, Vector3.Up );	

			if (rt.LengthSquared()<0.001f) {
				rt	=	Vector3.Cross( forward, Vector3.Right );
			}
			rt.Normalize();

			var up	=	Vector3.Cross( rt, forward );
			up.Normalize();

			var m	=	Matrix.Identity;
			m.Forward	=	forward;
			m.Right		=	rt;
			m.Up		=	up;
			
			SpawnFX( fxName, parentID, origin, velocity, Quaternion.RotationMatrix(m) );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxName"></param>
		/// <param name="parentID"></param>
		/// <param name="origin"></param>
		/// <param name="forward"></param>
		public void SpawnFX ( string fxName, uint parentID, Vector3 origin, Vector3 forward )
		{
			SpawnFX( fxName, parentID, origin, Vector3.Zero, forward );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxType"></param>
		/// <param name="position"></param>
		/// <param name="target"></param>
		/// <param name="orient"></param>
		public void SpawnFX ( string fxName, uint parentID, Vector3 origin )
		{
			SpawnFX( fxName, parentID, origin, Vector3.Zero, Quaternion.Identity );
		}


		
		/*-----------------------------------------------------------------------------------------
		 *	Entity stuff :
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="damage"></param>
		/// <param name="kickImpulse"></param>
		/// <param name="kickPoint"></param>
		/// <param name="damageType"></param>
		public void InflictDamage ( Entity entity, uint attackerID, short damage, Vector3 kickImpulse, Vector3 kickPoint, DamageType damageType )
		{
			entity?.Controller?.Damage( entity.ID, attackerID, damage, kickImpulse, kickPoint, damageType );
		}



		/// <summary>
		/// 
		/// </summary>
		void CommitKilledEntities ()
		{
            foreach ( var id in entityToKill ) {
				KillImmediatly( id );
			}
			
			entityToKill.Clear();			
		}
        

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		void KillImmediatly ( uint id )
		{
			if (id==0) {
				return;
			}

			Entity ent;

			if ( entities.TryGetValue(id, out ent)) {

				ent.DestroyRenderState(fxPlayback);
				EntityKilled?.Invoke( this, new EntityEventArgs(ent) );
				
				entities.Remove( id );
				ent?.Controller?.Killed();

			} else {
				Log.Warning("Entity #{0} does not exist", id);
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		public void Kill ( uint id )
		{
			LogTrace("kill: #{0}", id );
			entityToKill.Add( id );
		}



		/// <summary>
		/// 
		/// </summary>
		public void KillAll()
		{
			foreach ( var ent in entities ) {
				ent.Value.DestroyRenderState(fxPlayback);
				ent.Value.Controller?.Killed();
			}
			entities.Clear();
		}


		/// <summary>
		/// Writes world state to stream writer.
		/// </summary>
		/// <param name="writer"></param>
		public virtual void WriteToSnapshot ( Guid clientGuid, Stream stream )
		{
			var playerState = GetEntities()
				.Where( e1 => e1.UserGuid==clientGuid )
				.Where( e2 => e2.Controller is Character )
				.Select( e3 => (e3.Controller as Character).PlayerState )
				.FirstOrDefault();	

			playerState = playerState ?? PlayerState.NullState;

			snapshotWriter.Write( stream, ref environment, playerState, entities, fxEvents );
		}



		/// <summary>
		/// Reads world state from stream reader.
		/// </summary>
		/// <param name="writer"></param>
		public virtual void ReadFromSnapshot ( Stream stream, float lerpFactor )
		{
			snapshotReader.Read( stream, ref environment, playerState, entities, fxe=>fxPlayback?.RunFX(fxe,false), null, id=>KillImmediatly(id) );
		}



		/// <summary>
		/// Prints entire world state to console.
		/// </summary>
		public void PrintState ()
		{		
			var ents = entities.Select( pair => pair.Value ).OrderBy( e => e.ID ).ToArray();

			Log.Message("");
			Log.Message("---- World state ---- ");

			foreach ( var ent in ents ) {
				
				var id			=	ent.ID;
				var parent		=	ent.ParentID;
				var prefab		=	Atoms[ent.ClassID];
				var guid		=	ent.UserGuid;
				var controller	=	ent.Controller.GetType().Name;

				Log.Message("{0:X8} {1:X8} {2} {3,-32} {4,-32}", id, parent, guid, prefab, controller );
			}

			Log.Message("----------------" );
			Log.Message("");
		}

        void DrawDebugGrid()
        {
            if (Game.Keyboard.IsKeyDown(GridKey) && debugGridOff)
            {
                debugGridOn = false;
                Log.Warning($"Grid status was changed {debugGridOn}");
            }
            if (Game.Keyboard.IsKeyDown(GridKey) && !debugGridOff)
            {
                debugGridOn = true;
                Log.Warning($"Grid status was changed {debugGridOn}");
            }

            if (Game.Keyboard.IsKeyUp(GridKey))
            {
                if (debugGridOn)
                {
                    foreach (var vertex in vertices)
                    {
                        Game.RenderSystem.RenderWorld.Debug.DrawPoint(vertex.Vector, 1, vertex.Color, 5);
                    }

                    foreach (var edge in edges)
                    {
                        Game.RenderSystem.RenderWorld.Debug.DrawLine(edge.Start.Vector, edge.End.Vector, edge.Start.Color, edge.End.Color, 5, 5);
                    }

                    debugGridOff = true;
                }
                else debugGridOff = false;
            }
        }

        void GridUpdate()
        {
            //large, height, width
            vertices = new List<GridVertex>();
            var coordinateX = offsetX;
            var coordinateY = offsetY;
            var coordinateZ = offsetZ;
            for (var index0 = 0; index0 < large; index0++)
            {
                coordinateY = offsetY;
                for (var index1 = 0; index1 < height; index1++)
                {
                    coordinateX = offsetX;
                    for (var index2 = 0; index2 < width; index2++)
                    {
                        gridArray[index0, index1, index2] 
                            = new GridVertex() { Vector = new Vector3(coordinateX, coordinateY, coordinateZ), Value = coordinateX + coordinateY + coordinateZ };
                        vertices.Add(new GridVertex() { Vector = new Vector3(coordinateX, coordinateY, coordinateZ), Value = coordinateX + coordinateY + coordinateZ });
                        coordinateX += 4;
                    }
                    coordinateY += 4;
                }
                coordinateZ += 4;
            }

            GenerateGritEdges();
        }

        void GenerateGritEdges()
        {
            //large, height, width
            edges = new List<GridEdge>();
            var coordinateX = offsetX;
            var coordinateY = offsetY;
            var coordinateZ = offsetZ;
            for (var index0 = 0; index0 < large; index0++)
            {
                coordinateY = offsetY;
                for (var index1 = 0; index1 < height; index1++)
                {
                    coordinateX = offsetX;
                    for (var index2 = 0; index2 < width; index2++)
                    {
                        var item = gridArray[index0, index1, index2];
                        var x = item.Vector.X;
                        var y = item.Vector.Y;
                        var z = item.Vector.Z;
                        if (index0 + 1 < large)
                        {
                            edges.Add(new GridEdge() { Start = item, End = gridArray[index0 + 1, index1, index2] });
                        }
                        if (index1 + 1 < height)
                        {
                            edges.Add(new GridEdge() { Start = item, End = gridArray[index0, index1 + 1, index2] });
                        }
                        if (index2 + 1 < width)
                        {
                            edges.Add(new GridEdge() { Start = item, End = gridArray[index0, index1, index2 + 1] });
                        }
                        coordinateX += 4;
                    }
                    coordinateY += 4;
                }
                coordinateZ += 4;
            }
        }

    }
}
