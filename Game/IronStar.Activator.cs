using Fusion.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Engine.Audio;
using Fusion.Engine.Frames;
using Fusion.Engine.Tools;
using Fusion;
using Fusion.Core.Shell;
using IronStar.Editor;
using Fusion.Build;
using IronStar.SinglePlayer;
using IronStar.ECS;
using IronStar.Gameplay.Systems;
using Fusion.Engine.Graphics.GI;
using Fusion.Core.Content;
using System.IO;
using IronStar.AI;
using IronStar.UI.HUD;
using IronStar.Monsters.Systems;
using IronStar.Gameplay;
using IronStar.Editor.Systems;

namespace IronStar 
{
	partial class IronStar : Game
	{
		class TrackData
		{
			public long Bits;
		}

		class TrackSystem : ProcessingSystem<TrackData,Transform>
		{
			protected override TrackData Create( Entity entity, Transform component1 )
			{
				Log.Debug("ADD: {0}", entity.ToString());
				return new TrackData() { Bits = entity.ComponentMapping };
			}

			protected override void Destroy( Entity entity, TrackData resource )
			{
				Log.Debug("REM: {0}", entity.ToString());
			}

			protected override void Process( Entity entity, GameTime gameTime, TrackData resource, Transform component1 )
			{
				if (entity.ComponentMapping!=resource.Bits)
				{
					var oldBits	=	entity.ComponentMapping;
					var newBits	=	resource.Bits;
					var diff	=	oldBits ^ newBits;

					for (int i=0; i<64; i++)
					{
						long bit = 1L << i;

						if ((diff & bit) != 0)
						{
							if ( (newBits & bit) !=0 ) Log.Debug("ADD COMPONENT: {0}", ECSTypeManager.GetComponentType(bit).Name );
							if ( (oldBits & bit) !=0 ) Log.Debug("REM COMPONENT: {0}", ECSTypeManager.GetComponentType(bit).Name );
						}
					}

					resource.Bits = entity.ComponentMapping;
				}
			}
		}

		public static IGameState CreateGameState( Game game, ContentManager content, string mapName, Mapping.Map mapContent = null, MapEditor editor = null )
		{
			var isEditor	=	mapContent!=null;
			var map			=	mapContent ?? content.Load<Mapping.Map>(@"maps\" + mapName);
			var gs			=	new GameState(game, content, false);
			var gs2			=	new GameState(game, content, true );
			var rs			=	game.RenderSystem;

			var rw			=	game.RenderSystem.RenderWorld;

			gs.Paused		=	(editor!=null);
			
			//	physics and FX systems are used by many other systems :
			var physicsCore = new ECSPhysics.PhysicsCore();
			var fxPlayback	= new SFX.FXPlayback(game, content );

			gs.Services.AddService( content );
			gs.Services.AddService( game.RenderSystem );

			var commandQueue		=	new UserCommandQueue();
			var playerInputMaster	=	new PlayerInputSystem( commandQueue, true );
			var playerInputSlave	=	new PlayerInputSystem( commandQueue, false );

			//	player system :
			gs.AddSystem( playerInputSlave );

			gs.AddSystem( new PlayerSpawnSystem() );

			//	weapon system :
			gs.AddSystem( new WeaponSystem(gs, physicsCore, fxPlayback ) );
			gs.AddSystem( new ProjectileSystem(gs, physicsCore ) );

			//	physics simulation :
			gs.AddSystem( physicsCore );
			gs.AddSystem( new ECSPhysics.StaticCollisionSystem(physicsCore) );
			gs.AddSystem( new ECSPhysics.DynamicCollisionSystem(physicsCore ) );
			gs.AddSystem( new ECSPhysics.CharacterControllerSystem(physicsCore ) );

			//	attachment system :
			gs.AddSystem( new AttachmentSystem() );

			//	game logic :
			gs.AddSystem( new HealthSystem() );
			gs.AddSystem( new PickupSystem() );
			gs.AddSystem( new ExplosionSystem() );

			//	AI :
			gs.AddSystem( new PerceptionSystem(physicsCore) );
			gs.AddSystem( new BehaviorSystem(physicsCore ) );
			gs.AddSystem( new NavigationSystem() );
			gs.AddSystem( new MonsterKillSystem() );

			//	animation systems :
			gs.AddSystem( new Gameplay.BobbingSystem() );
			gs.AddSystem( new StepSystem() );
			gs.AddSystem( new MonsterAnimationSystem(game,fxPlayback,physicsCore) );
			gs.AddSystem( new SFX.FXTracker() );

			// rendering :
			//gs2.AddSystem( new TrackSystem() );
			gs2.AddSystem( playerInputMaster );
			gs2.AddSystem( new Gameplay.CameraSystem(fxPlayback, playerInputMaster) );
			gs2.AddSystem( new FPVWeaponSystem(game) );
			gs2.AddSystem( fxPlayback );

			gs2.AddSystem( new SFX2.RenderModelSystem(game) );
			gs2.AddSystem( new SFX2.DecalSystem(game.RenderSystem		) );
			gs2.AddSystem( new SFX2.OmniLightSystem(game.RenderSystem	) );
			gs2.AddSystem( new SFX2.SpotLightSystem(game.RenderSystem	) );
			gs2.AddSystem( new SFX2.LightProbeSystem(game.RenderSystem	) );
			gs2.AddSystem( new SFX2.LightVolumeSystem(game.RenderSystem	) );
			gs2.AddSystem( new SFX2.LightingSystem() );

			//	ui
			gs2.AddSystem( new GameFXSystem(game) );
			gs2.AddSystem( new HudSystem(game) );
			//*/

			if (isEditor)
			{
				gs.GetService<CameraSystem>().Enabled = false;

				gs.AddSystem( new EditorEntityRenderSystem( editor, rs.RenderWorld.Debug ) );
				gs.AddSystem( new EditorLightRenderSystem( editor, rs.RenderWorld.Debug ) );
				gs.AddSystem( new EditorPhysicsRenderSystem( editor, rs.RenderWorld.Debug ) );
				gs.AddSystem( new EditorModelRenderSystem( editor, rs.RenderWorld.Debug ) );
				gs.AddSystem( new EditorCharacterRenderSystem( editor, rs.RenderWorld.Debug ) );  //*/
			}

			map.ActivateGameState(gs);

			LoadContent(rw, content, mapName);
			gs.Reloading += (s,e) => LoadContent( rw, content, mapName );

			//return gs;
			return new MTGameState( game, gs, gs2, TimeSpan.FromSeconds(1.0f/60.0f) );
		}


		static void LoadContent( RenderWorld rw, ContentManager content, string mapName )
		{
			rw.VirtualTexture		=	content.Load<VirtualTexture>("*megatexture");
			rw.LightSet.SpotAtlas	=	content.Load<TextureAtlas>(@"spots\spots|srgb");
			rw.LightSet.DecalAtlas	=	content.Load<TextureAtlas>(@"decals\decals");

			rw.LightProbes			=	content.Load(Path.Combine(RenderSystem.LightProbePath, mapName ), (LightProbeHDRI)null );
			rw.LightMap				=	content.Load(Path.Combine(RenderSystem.LightmapPath, mapName), (LightMap)null );
		}
	}
}
