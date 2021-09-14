﻿using Fusion.Core;
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
using IronStar.ECSGraphics;

namespace IronStar 
{
	partial class IronStar : Game
	{
		public static GameState CreateGameState( Game game, ContentManager content, string mapName, Mapping.Map mapContent = null )
		{
			var isEditor	=	mapContent!=null;
			var map			=	mapContent ?? content.Load<Mapping.Map>(@"maps\" + mapName);
			var gs			=	new GameState(game, content, TimeSpan.FromMilliseconds(100));

			var rw	=	game.RenderSystem.RenderWorld;

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
			gs.AddSystem( new ExplosionSystem() );
			gs.AddSystem( new WeaponSystem(gs, physicsCore, fxPlayback) );
			gs.AddSystem( new ProjectileSystem(gs, physicsCore) );

			//	AI :
			gs.AddSystem( new PerceptionSystem(physicsCore) );
			gs.AddSystem( new BehaviorSystem(physicsCore) );
			gs.AddSystem( new NavigationSystem() );
			gs.AddSystem( new MonsterKillSystem() );

			//	animation systems :
			gs.AddSystem( new StepSystem() );
			gs.AddSystem( new Gameplay.BobbingSystem() );
			gs.AddSystem( new Gameplay.CameraSystem(fxPlayback) );
			/*gs.AddSystem( new FPVWeaponSystem(game) );
			gs.AddSystem( new MonsterAnimationSystem(game,fxPlayback,physicsCore) );*/

			/*
			gs.AddSystem( fxPlayback );
			*/

			//	rendering :
			
			gs.AddSystem( new SFX2.RenderModelSystem(game) );
			/*
			gs.AddSystem( new SFX2.DecalSystem(game.RenderSystem) );
			gs.AddSystem( new SFX2.OmniLightSystem(game.RenderSystem) );
			gs.AddSystem( new SFX2.SpotLightSystem(game.RenderSystem) );
			gs.AddSystem( new SFX2.LightProbeSystem(game.RenderSystem) );
			gs.AddSystem( new SFX2.LightVolumeSystem(game.RenderSystem) );
			gs.AddSystem( new BillboardSystem(fxPlayback) );
			*/
			gs.AddSystem( new SFX2.LightingSystem() );

			gs.AddSystem( new Gameplay.PlayerSystem() );

			//	ui
			//gs.AddSystem( new GameFXSystem(game) );
			gs.AddSystem( new HudSystem(game) );
			//*/

			if (isEditor)
			{
				gs.GetService<Gameplay.CameraSystem>().Enabled = false;
			}

			map.ActivateGameState(gs);

			LoadContent(rw, content, mapName);
			gs.Reloading += (s,e) => LoadContent( rw, content, mapName );

			gs.Start();

			return gs;
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
