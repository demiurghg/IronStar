﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Content;
using Fusion.Engine.Server;
using Fusion.Engine.Client;
using Fusion.Core.Extensions;
using IronStar.SFX;
using Fusion.Core.IniParser.Model;
using Fusion.Engine.Graphics;
using IronStar.Mapping;
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

		public readonly IMessageService MessageService;

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


		public SnapshotHeader snapshotHeader = new SnapshotHeader();

		public readonly bool IsPresentationEnabled;

		Map map;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public GameWorld( Game game, Map map, ContentManager content, IMessageService msgsvc, bool enablePresentation, Guid userGuid )
		{
			IsPresentationEnabled	=	enablePresentation;
			MessageService			=	msgsvc;

			this.Content	=	content;
			this.Game		=	game;
			this.map		=	map;
			this.UserGuid	=	userGuid;

			entities		=	new EntityCollection();
			physics			=	new PhysicsManager( this, 16 );

			//	setup rendering stuff :
			if (enablePresentation) {

				var rw					=	Game.RenderSystem.RenderWorld;

				rw.VirtualTexture		=	Content.Load<VirtualTexture>("*megatexture");
				fxPlayback				=	new SFX.FXPlayback( this );
				modelManager			=	new SFX.ModelManager( this );

				rw.LightSet.SpotAtlas	=	Content.Load<TextureAtlas>(@"spots\spots");
				rw.LightSet.DecalAtlas	=	Content.Load<TextureAtlas>(@"decals\decals");
			}

			//	initialize server atoms, 
			//	including assets and inline-factories.
			InitServerAtoms();

			//	spawn map nodes: static models, lights, 
			//	fx, entities, etc.
			foreach ( var mapNode in map.Nodes ) {
				mapNode.SpawnNode(this);
			}
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {

				Content?.Dispose();

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
		public virtual void SimulateWorld ( GameTime gameTime )
		{
			UpdatePlayers( gameTime.ElapsedSec );

			physics.Update( gameTime.ElapsedSec );
				
			//
			//	Control entities :
			//
			ForEachEntity( e => e?.Update( gameTime ) );

			//
			//	Kill entities :
			//
			CommitKilledEntities();
		}



		/// <summary>
		/// Updates visual and audial stuff
		/// </summary>
		/// <param name="gameTime"></param>
		public void PresentWorld ( GameTime gameTime, float lerpFactor, GameCamera gameCamera, UserCommand userCmd )
		{
			var dr = Game.RenderSystem.RenderWorld.Debug;
			var rw = Game.RenderSystem.RenderWorld;

			var visibleEntities = entities.Select( pair => pair.Value ).ToArray();

			//
			//	draw all entities :
			//
			foreach ( var entity in visibleEntities ) {
				entity?.Draw( gameTime, EntityFX.None );
			}

			//
			//	update effects :
			//	
			foreach ( var fxe in fxEvents ) {
				fxPlayback.RunFX( fxe, false );
			}
			fxEvents.Clear();


			fxPlayback.Update( gameTime, lerpFactor );
			modelManager.Update( gameTime, lerpFactor, gameCamera, userCmd );

			//
			//	update environment :
			//
			rw.HdrSettings.BloomAmount			= 0.1f;
			rw.HdrSettings.DirtAmount			= 0.0f;
			rw.HdrSettings.KeyValue				= 0.18f;

			rw.SkySettings.SunPosition			=	snapshotHeader.SunDirection;
			rw.SkySettings.SunLightIntensity	=	snapshotHeader.SunIntensity;
			rw.SkySettings.SkyTurbidity			=	snapshotHeader.Turbidity;
			rw.SkySettings.SkyIntensity			=	0.5f;

			rw.FogSettings.VisibilityDistance	=	snapshotHeader.FogDistance;
			rw.FogSettings.Color				=	snapshotHeader.FogColor;

			rw.LightSet.DirectLight.Direction	=	rw.SkySettings.SunLightDirection;
			rw.LightSet.DirectLight.Intensity	=	rw.SkySettings.SunLightColor;

			rw.LightSet.AmbientLevel			=	snapshotHeader.AmbientLevel;

		}


		/*-----------------------------------------------------------------------------------------
		 *	Entity creation
		-----------------------------------------------------------------------------------------*/

		EntityFactory FindFactory( string classname )
		{
			var factoryNode = map.Nodes.FirstOrDefault( n1 => (n1 as MapEntity)?.FactoryName == classname );

			if (factoryNode==null) {
				return Content.Load(@"entities\" + classname, (EntityFactory)null );
			} else {
				return (factoryNode as MapEntity).Factory;
			}
		}


		/// <summary>
		/// Creates entity by class name.
		/// </summary>
		/// <param name="classname"></param>
		/// <returns></returns>
		public Entity Spawn ( string classname )
		{
			var classId	=	Atoms[classname];
			var factory	=	FindFactory( classname );

			//	get ID :
			uint id = idCounter;

			idCounter++;

			//	this actually will never happen, about 103 day of intense playing.
			if ( idCounter==0 ) {
				throw new InvalidOperationException( "Too much entities were spawned" );
			}

			//	Create instance.
			var entity = factory.Spawn( id, classId, this );

			entities.Add( id, entity );

			EntitySpawned?.Invoke( this, new EntityEventArgs( entity ) );

			return entity;
		}



		/// <summary>
		/// Creates entity by class ID
		/// </summary>
		/// <param name="classId"></param>
		/// <returns></returns>
		public Entity Spawn ( short classId )
		{
			var classname	=	Atoms[classId];

			return Spawn( classname );
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
		public void InflictDamage ( Entity entity, Entity attacker, short damage, DamageType damageType, Vector3 kickImpulse, Vector3 kickPoint )
		{
			entity?.Damage( attacker, damage, damageType, kickImpulse, kickPoint );
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

				EntityKilled?.Invoke( this, new EntityEventArgs(ent) );
				entities.Remove( id );
				ent.Kill();

			} else {
				Log.Warning("KillImmediatly: Entity #{0} does not exist", id);
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
				ent.Value.Kill();
			}
			entities.Clear();
		}


		/// <summary>
		/// Writes world state to stream writer.
		/// </summary>
		/// <param name="writer"></param>
		public virtual void WriteToSnapshot ( Guid clientGuid, Stream stream )
		{
			var playerCharacter = GetPlayerCharacter( clientGuid );

			snapshotHeader.ClearHud();
			//playerCharacter?.UpdateHud( snapshotHeader );

			snapshotWriter.Write( stream, snapshotHeader, entities, fxEvents );
		}



		/// <summary>
		/// Reads world state from stream reader.
		/// </summary>
		/// <param name="writer"></param>
		public virtual void ReadFromSnapshot ( Stream stream, float lerpFactor )
		{
			snapshotReader.Read( 
				this,
				stream, snapshotHeader, entities, 
				fxe => fxPlayback?.RunFX(fxe,false), 
				ent => EntitySpawned?.Invoke( this, new EntityEventArgs(ent)),
				id  => KillImmediatly(id) 
			);
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
				var entity		=	ent.GetType().Name;

				Log.Message("{0:X8} {1:X8} {2} {3,-32} {4,-32}", id, parent, guid, prefab, entity );
			}

			Log.Message("----------------" );
			Log.Message("");
		}
	}
}
