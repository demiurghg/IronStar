using System;
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
using IronStar.Items;
using IronStar.Entities.Players;
using Native.NRecast;
using Fusion.Engine.Graphics.Lights;

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
		public readonly bool EditorMode;

		public delegate void EntityEventHandler ( object sender, EntityEventArgs e );

		List<uint> entityToKill = new List<uint>();

		public EntityCollection entities;
		public ItemCollection items;
		uint entityIdCounter = 1;
		uint itemIdCounter = 1;

		public event EntityEventHandler EntitySpawned;
		public event EntityEventHandler EntityKilled;

		List<FXEvent> fxEvents = new List<FXEvent>();

		SFX.FXPlayback		fxPlayback = null;
		SFX.ModelManager	modelManager = null;
		PhysicsManager		physics	= null;
		NavigationMesh		navMesh = null;

		public SFX.ModelManager	ModelManager { get { return modelManager; } }
		public SFX.FXPlayback	FXPlayback   { get { return fxPlayback; } }
		public PhysicsManager	Physics		{ get { return physics; } }
		public EntityCollection	Entities { get { return entities; } }
		public ItemCollection	Items	 { get { return items; } }
		public NavigationMesh	NavMesh { get { return navMesh; } }

		public SnapshotHeader snapshotHeader = new SnapshotHeader();

		Map map;
		string mapName;



		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public GameWorld( Game game, string mapName, Map map, ContentManager content, IMessageService msgsvc, Guid userGuid, bool editor )
		{
			MessageService	=	msgsvc;

			this.EditorMode	=	editor;

			this.Content	=	content;
			this.Game		=	game;
			this.map		=	map;
			this.UserGuid	=	userGuid;
			this.mapName	=	mapName;

			entities		=	new EntityCollection();
			items			=	new ItemCollection(this);
			physics			=	new PhysicsManager( this, 48 );

			try {
				navMesh			=	map.BuildNavMesh( content );
			} catch ( Exception e ) {
				Log.Error(e.Message);
			}

			//	setup rendering stuff :
			var rw					=	Game.RenderSystem.RenderWorld;
			rw.ClearWorld();

			rw.VirtualTexture		=	Content.Load<VirtualTexture>("*megatexture");
			fxPlayback				=	new SFX.FXPlayback( this );
			modelManager			=	new SFX.ModelManager( this );

			rw.LightSet.SpotAtlas	=	Content.Load<TextureAtlas>(@"spots\spots|srgb");
			rw.LightSet.DecalAtlas	=	Content.Load<TextureAtlas>(@"decals\decals");

			//	initialize server atoms, 
			//	including assets and inline-factories.
			InitServerAtoms();

			//	spawn map nodes: static models, lights, 
			//	fx, entities, etc.
			foreach ( var mapNode in map.Nodes ) {
				mapNode.SpawnNode(this);
			}

			Game.Reloading +=Game_Reloading;

			Game_Reloading(this, EventArgs.Empty);

			map.UpdateEnvironment(this);
		}


		private void Game_Reloading( object sender, EventArgs e )
		{
			var rw					=	Game.RenderSystem.RenderWorld;

			rw.IrradianceCache		=	Content.Load(Path.Combine(RenderSystem.LightmapPath, mapName + "_irrcache"	), (LightProbeGBufferCache)null );
			Game.RenderSystem.Radiosity.LightMap	=	Content.Load(Path.Combine(RenderSystem.LightmapPath, mapName + "_irrmap"), (LightMap)null );

			//Game.RenderSystem.RayTracer.BuildAccelerationStructure();
		}

		
		public void RefreshWorld ()
		{
			//navMesh			=	map.BuildNavMesh( content );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {

				Content?.Dispose();

				Game.Reloading-=Game_Reloading;

				Game.RenderSystem.RenderWorld.ClearWorld();

				SafeDispose( ref fxPlayback );
				SafeDispose( ref modelManager );
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
			physics.Update( gameTime.ElapsedSec );
				
			//	update entitites :
			ForEachEntity( e => e?.Update( gameTime ) );

			//	update items :
			foreach ( var i in items ) {
				i.Value.Update( gameTime );
			}

			//	kill entities & items :
			CommitKilledEntities();
			items.RemoveAll( (id,item) => item.Owner==0 || !IsAlive(item.Owner) );
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
				entity?.Draw( gameTime );
				entity?.UpdatePresentation( fxPlayback, modelManager, gameCamera );
			}

			//
			//	update effects :
			//	
			foreach ( var fxe in fxEvents ) {
				fxPlayback.RunFX( fxe, false );
			}
			fxEvents.Clear();


			modelManager.Update( gameTime, lerpFactor, gameCamera, userCmd );
			fxPlayback.Update( gameTime, lerpFactor );

			//
			//	update environment :
			//
			rw.HdrSettings.BloomAmount			= 0.1f;
			rw.HdrSettings.DirtAmount			= 0.0f;
			rw.HdrSettings.KeyValue				= 0.18f;

			var rs	=	Game.GetService<RenderSystem>();
			rw.LightSet.DirectLight.Direction	=	-rs.Sky.GetSunDirection();
			rw.LightSet.DirectLight.Intensity	=	 rs.Sky.GetSunIntensity(true);	
		}


		/*-----------------------------------------------------------------------------------------
		 *	Entity creation
		-----------------------------------------------------------------------------------------*/

		public EntityFactory GetFactoryByName( string classname )
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
		public Entity Spawn ( string classname, uint id=0 )
		{
			var classId	=	Atoms[classname];
			var factory	=	GetFactoryByName( classname );

			//	get ID :
			if (id==0) {
				id = entityIdCounter;
				entityIdCounter++;
			}

			//	this actually will never happen, about 103 day of intense playing.
			if ( entityIdCounter==0 ) {
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
		public Entity Spawn ( short classId, uint id=0 )
		{
			var classname	=	Atoms[classId];

			return Spawn( classname );
		}
		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="classname"></param>
		/// <returns></returns>
		public Item SpawnItem ( string classname, uint owner, uint id=0 )
		{
			//	get ID :
			if (id==0) {
				id = itemIdCounter | 0x80000000;	// to not to overlap with entities
				itemIdCounter++;
			}

			var clsid	=	Atoms[classname];
			var factory =	Content.Load(@"items\" + classname, (ItemFactory)null );
			var item	=	factory?.Spawn( id, clsid, this );

			if (item!=null) {
				item.Owner	=	owner;
				items.Add( id, item );
			}

			return item;
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
			var m = MathUtil.ComputeAimedBasis( forward );
			
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
		public void InflictDamage ( Entity entity, uint attackerId, int damage, DamageType damageType, Vector3 kickImpulse, Vector3 kickPoint )
		{
			var attacker = GetEntity( attackerId );

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
		/// Immediatly removes given entity.
		/// Never call the method from Entity.Update!
		/// </summary>
		/// <param name="id"></param>
		public void KillImmediatly ( uint id )
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
		/// Adds entity to kill-list.
		/// Entity will be killed at the end of the game update.
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
		/// <param name="predicate"></param>
		public void Kill ( Func<Entity,bool> predicate )
		{
			foreach ( var ent in entities ) {
				if ( predicate(ent.Value) ) {
					Kill( ent.Value.ID );
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public void KillAll()
		{
			foreach ( var ent in entities ) {
				ent.Value?.Kill();
			}
			entities.Clear();
			items.Clear();
		}


		/// <summary>
		/// Writes world state to stream writer.
		/// </summary>
		/// <param name="writer"></param>
		public virtual void WriteToSnapshot ( Guid clientGuid, Stream stream )
		{
			var playerCharacter = GetPlayerCharacter( clientGuid );

			snapshotWriter.Write( stream, snapshotHeader, entities, items, fxEvents );
		}



		/// <summary>
		/// Reads world state from stream reader.
		/// </summary>
		/// <param name="writer"></param>
		public virtual void ReadFromSnapshot ( Stream stream, float lerpFactor )
		{
			snapshotReader.Read( 
				this,
				stream, snapshotHeader, entities, items,
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
