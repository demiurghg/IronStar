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
using IronStar.Gameplay.Components;

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

			UpdateHealthStatus( gs, gameTime, player );
			UpdateWeaponStatus( gs, gameTime, player );
		}



		void UpdateWeaponStatus( IGameState gs, GameTime gameTime, Entity player )
		{
			var inventory	=	player?.GetComponent<InventoryComponent>();

			#warning TODO HUD
			/*if (inventory!=null)
			{
				var weapon	=	inventory.ActiveWeapon?.GetComponent<WeaponComponent>();

				AmmoComponent ammo;
				NameComponent ammoName;
				inventory.FindItem(gs, (a,n) => n.Name == weapon?.AmmoClass, out ammo, out ammoName );

				if (weapon!=null)
				{
					hudFrame.Ammo.Visible	=	true;
					hudFrame.Ammo.Value		=	ammo==null ? 0 : ammo.Count;
					hudFrame.Ammo.MaxValue	=	ammo==null ? 0 : ammo.Capacity;
				}
				else
				{
					hudFrame.Ammo.Visible = false;
				}
			} */
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
