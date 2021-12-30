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
using Fusion.Core.Extensions;
using IronStar.Gameplay.Weaponry;
using IronStar.Environment;

namespace IronStar.Gameplay.Systems
{
	class FPVWeaponSystem : DisposableBase, ISystem
	{
		const string ANIM_TILT		=	"tilt"			;
		const string ANIM_IDLE		=	"idle"			;
		const string ANIM_WARMUP	=	"warmup"		;
		const string ANIM_COOLDOWN	=	"cooldown"		;
		const string ANIM_LANDING	=	"landing"		;
		const string ANIM_JUMP		=	"jump"			;
		const string ANIM_SHAKE		=	"shake"			;
		const string ANIM_WALKLEFT	=	"step_left"		;
		const string ANIM_WALKRIGHT	=	"step_right"	;
		const string ANIM_FIRSTLOOK	=	"examine"		;
		const string ANIM_RAISE		=	"raise"			;
		const string ANIM_DROP		=	"drop"			;

		const string SOUND_NO_AMMO	=	"weapon/noAmmo"	;

		const string JOINT_MUZZLE	=	"muzzle"		;
		const string CAMERA_NODE	=	"camera1"		;


		public void Add( IGameState gs, Entity e ) {}
		public void Remove( IGameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }

		readonly Aspect playerAspect	=	new Aspect().Include<PlayerComponent,CharacterController,StepComponent>()
											.Include<InventoryComponent>()
											;

		RenderModelInstance	renderModel = null;
		WeaponType activeWeapon = WeaponType.None;

		WeaponAnimator		animator;


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


		public void Update( IGameState gs, GameTime gameTime )
		{
			var rs = gs.Game.GetService<RenderSystem>();
			var rw = rs.RenderWorld;

			var playerEntity	=	gs.QueryEntities(playerAspect).LastOrDefault();

			if (playerEntity!=null)
			{
				var inventory		=	playerEntity.GetComponent<InventoryComponent>();
				var steps			=	playerEntity.GetComponent<StepComponent>();
				var uc				=	playerEntity.GetComponent<UserCommandComponent>();
				var weaponState		=	playerEntity.GetComponent<WeaponStateComponent>();

				if (activeWeapon!=weaponState.ActiveWeapon)
				{
					activeWeapon = weaponState.ActiveWeapon;
					ChangeWeaponModel( gs, activeWeapon );
				}

				var cameraMatrix = rw.Camera.CameraMatrix;

				animator?.Update( gameTime, rw.Camera.CameraMatrix, weaponState, steps, uc );  
				renderModel?.SetTransforms( rw.Camera.CameraMatrix, animator?.Transforms, true );
			}
		}


		void ChangeWeaponModel( IGameState gs, WeaponType weaponType )
		{
			var rs = gs.Game.GetService<RenderSystem>();
			var rw = rs.RenderWorld;
			var fx = gs.GetService<SFX.FXPlayback>();

			renderModel?.RemoveInstances();
			animator	=	null;

			if (weaponType!=WeaponType.None)
			{
				var weapon = Arsenal.Get(weaponType);
				renderModel	=	new RenderModelInstance( gs, weapon.FPVRenderModel, rw.Camera.CameraMatrix, CAMERA_NODE );
				animator	=	new WeaponAnimator(fx, renderModel); 
				renderModel.AddInstances();
			}
		}
	}
}
