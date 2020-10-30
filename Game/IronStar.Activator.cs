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
using Fusion.Engine.Graphics.Lights;
using Fusion.Core.Content;
using System.IO;
using IronStar.AI;
using IronStar.UI.HUD;
using IronStar.Monsters.Systems;

namespace IronStar {
	partial class IronStar : Game
	{
		public static GameState CreateGameState( Game game, ContentManager content, string mapName, Mapping.Map mapContent = null )
		{
			var isEditor	=	mapContent!=null;
			var map			=	mapContent ?? content.Load<Mapping.Map>(@"maps\" + mapName);
			var gs			=	new GameState(game, content);

			var rw	=	game.RenderSystem.RenderWorld;
			rw.VirtualTexture		=	content.Load<VirtualTexture>("*megatexture");
			rw.LightSet.SpotAtlas	=	content.Load<TextureAtlas>(@"spots\spots|srgb");
			rw.LightSet.DecalAtlas	=	content.Load<TextureAtlas>(@"decals\decals");

			rw.IrradianceCache						=	content.Load(Path.Combine(RenderSystem.LightmapPath, mapName + "_irrcache"	), (LightProbeGBufferCache)null );
			game.RenderSystem.Radiosity.LightMap	=	content.Load(Path.Combine(RenderSystem.LightmapPath, mapName + "_irrmap"), (LightMap)null );

			gs.Services.AddService( content );
			gs.Services.AddService( game.RenderSystem );

			//	physics simulation :
			var physicsCore = new ECSPhysics.PhysicsCore();
			var fxPlayback	= new SFX.FXPlayback(game, content);
			gs.AddSystem( new ECSPhysics.StaticCollisionSystem(physicsCore) );
			gs.AddSystem( new ECSPhysics.DynamicCollisionSystem(physicsCore) );
			gs.AddSystem( new ECSPhysics.CharacterControllerSystem(physicsCore) );
			gs.AddSystem( physicsCore );

			//	attachment system :
			gs.AddSystem( new AttachmentSystem() );

			//	game logic :
			gs.AddSystem( new HealthSystem() );
			gs.AddSystem( new PickupSystem() );
			gs.AddSystem( new WeaponSystem(physicsCore) );
			gs.AddSystem( new ProjectileSystem(physicsCore) );

			//	AI :
			gs.AddSystem( new PerceptionSystem(physicsCore) );
			gs.AddSystem( new BehaviorSystem(physicsCore) );
			gs.AddSystem( new NavigationSystem() );
			gs.AddSystem( new MonsterKillSystem() );

			//	animation systems :
			gs.AddSystem( new StepSystem() );
			gs.AddSystem( new Gameplay.BobbingSystem() );
			gs.AddSystem( new Gameplay.CameraSystem(fxPlayback) );
			gs.AddSystem( new FPVWeaponSystem(game) );
			gs.AddSystem( new MonsterAnimationSystem(game,fxPlayback,physicsCore) );
			gs.AddSystem( fxPlayback );

			//	rendering :
			gs.AddSystem( new SFX2.RenderModelSystem(game) );
			gs.AddSystem( new SFX2.DecalSystem(game.RenderSystem) );
			gs.AddSystem( new SFX2.OmniLightSystem(game.RenderSystem) );
			gs.AddSystem( new SFX2.SpotLightSystem(game.RenderSystem) );
			gs.AddSystem( new SFX2.LightProbeSystem(game.RenderSystem) );
			gs.AddSystem( new SFX2.LightingSystem() );
			gs.AddSystem( new Gameplay.PlayerSystem() );

			//	ui
			gs.AddSystem( new GameFXSystem(game) );
			gs.AddSystem( new HudSystem(game) );


			if (isEditor)
			{
				gs.GetService<Gameplay.CameraSystem>().Enabled = false;
			}


			map.ActivateGameState(gs);

			return gs;
		}
	}
}
