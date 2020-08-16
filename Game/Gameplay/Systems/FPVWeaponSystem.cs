using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Engine.Graphics;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Gameplay.Components;
using IronStar.SFX2;
using IronStar.Animation;

namespace IronStar.Gameplay.Systems
{
	class FPVWeaponSystem : DisposableBase, ISystem
	{
		const string CameraNodeName = "camera1";

		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }

		readonly Aspect playerAspect	=	new Aspect().Include<PlayerComponent,CharacterController,StepComponent>()
											.Include<InventoryComponent>()
											;

		readonly Aspect weaponAspect	=	new Aspect().Include<WeaponComponent>();

		AnimationComposer composer;
		RenderModelInstance	renderModel = null;
		Entity activeWeapon = null;


		public FPVWeaponSystem( Game game )
		{
		}


		protected override void Dispose( bool disposing )
		{
			base.Dispose( disposing );

			if (disposing)
			{
				
			}
		}


		public void Update( GameState gs, GameTime gameTime )
		{
			var rs = gs.GetService<RenderSystem>();
			var rw = rs.RenderWorld;

			var playerEntity	=	gs.QueryEntities(playerAspect).LastOrDefault();

			if (playerEntity!=null)
			{
				var inventory		=	playerEntity.GetComponent<InventoryComponent>();

				var weaponEntity	=	gs.GetEntity( inventory.ActiveItemID );
				var weapon			=	weaponEntity?.GetComponent<WeaponComponent>();
				var	model			=	weaponEntity?.GetComponent<RenderModel>();

				if (activeWeapon!=weaponEntity)
				{
					ChangeWeaponModel( gs, model );
				}

				if (renderModel!=null)
				{
					renderModel.WorldMatrix = rw.Camera.CameraMatrix;
				}
			}
		}


		void ChangeWeaponModel( GameState gs, RenderModel model )
		{
			var rs = gs.GetService<RenderSystem>();
			var rw = rs.RenderWorld;

			renderModel?.Dispose();
			composer	=	null;

			if (model!=null)
			{
				renderModel	=	new RenderModelInstance( gs, model, rw.Camera.CameraMatrix, CameraNodeName );
				composer	=	new AnimationComposer( gs.GetService<SFX.FXPlayback>(), renderModel, renderModel.Scene );
			}
		}


	}
}
