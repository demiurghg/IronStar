using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using IronStar.ECS;
using IronStar.ECSFactories;
using IronStar.Environment;
using IronStar.Gameplay;
using IronStar.Gameplay.Components;
using IronStar.Gameplay.Weaponry;

namespace IronStar.UI.HUD
{
	public class HudSystem : ISystem
	{
		readonly Game Game;

		readonly HudFrame hudFrame;


		public HudSystem ( Game game )
		{
			this.Game	=	game;
			hudFrame	=	(game.GetService<UserInterface>().Instance as ShooterInterface)?.HudFrame;
		}

		public void Add( IGameState gs, Entity e ) {}
		public void Remove( IGameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }

		public void Update( IGameState gs, GameTime gameTime )
		{
			var player	=	gs.QueryEntities(PlayerFactory.PlayerAspect).LastOrDefault();

			UpdateCrosshair( gs, gameTime, player );
			UpdateHealthStatus( gs, gameTime, player );
			UpdateWeaponStatus( gs, gameTime, player );
		}


		void UpdateCrosshair( IGameState gs, GameTime gameTime, Entity player )
		{
			var guiSystem	=	gs.GetService<GUISystem>();

			if (guiSystem!=null)
			{
				hudFrame.CrossHair.Visible	=	!guiSystem.Engaged;
			}
		}


		void UpdateWeaponStatus( IGameState gs, GameTime gameTime, Entity player )
		{
			var inventory	=	player?.GetComponent<InventoryComponent>();
			var wpnState	=	player?.GetComponent<WeaponStateComponent>();

			hudFrame.Ammo.Visible	=	false;

			if (inventory!=null && wpnState!=null && wpnState.ActiveWeapon!=WeaponType.None)
			{
				var weapon	=	Arsenal.Get( wpnState.ActiveWeapon );
				var ammo	=	Arsenal.Get( weapon.AmmoType );

				if (ammo!=null)
				{
					hudFrame.Ammo.Visible	=	true;
					hudFrame.Ammo.Value		=	inventory.GetAmmoCount( weapon.AmmoType );
					hudFrame.Ammo.MaxValue	=	ammo.Capacity;
				}
			}
		}


		void UpdateHealthStatus( IGameState gs, GameTime gameTime, Entity player )
		{
			var health	=	player?.GetComponent<HealthComponent>();

			if (health!=null)
			{
				hudFrame.Health.Visible		=	true;
				hudFrame.Armor.Visible		=	true;

				hudFrame.Health.Value		=	health.Health;
				hudFrame.Health.MaxValue	=	health.MaxHealth;

				hudFrame.Armor.Value		=	health.Armor;
				hudFrame.Armor.MaxValue		=	health.MaxArmor;
			}
			else
			{
				hudFrame.Health.Visible		=	false;
				hudFrame.Armor.Visible		=	false;
			}
		}
	}
}
