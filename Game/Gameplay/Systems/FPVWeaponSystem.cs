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

		const string SOUND_LANDING	=	"player/landing";
		const string SOUND_STEP		=	"player/step"	;
		const string SOUND_JUMP		=	"player/jump"	;
		const string SOUND_NO_AMMO	=	"weapon/noAmmo"	;

		const string JOINT_MUZZLE	=	"muzzle"		;
		const string CAMERA_NODE	=	"camera1"		;


		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }

		readonly Aspect playerAspect	=	new Aspect().Include<PlayerComponent,CharacterController,StepComponent>()
											.Include<InventoryComponent>()
											;

		readonly Aspect weaponAspect	=	new Aspect().Include<WeaponComponent>();

		RenderModelInstance	renderModel = null;
		Entity activeWeapon = null;

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


		public void Update( GameState gs, GameTime gameTime )
		{
			var rs = gs.GetService<RenderSystem>();
			var rw = rs.RenderWorld;

			var playerEntity	=	gs.QueryEntities(playerAspect).LastOrDefault();

			if (playerEntity!=null)
			{
				var inventory		=	playerEntity.GetComponent<InventoryComponent>();
				var steps			=	playerEntity.GetComponent<StepComponent>();

				var weaponEntity	=	gs.GetEntity( inventory.ActiveWeaponID );
				var weapon			=	weaponEntity?.GetComponent<WeaponComponent>();
				var	model			=	weaponEntity?.GetComponent<RenderModel>();

				if (activeWeapon!=weaponEntity)
				{
					ChangeWeaponModel( gs, model );
					activeWeapon = weaponEntity;
				}

				if (renderModel!=null)
				{
					renderModel.SetTransform( rw.Camera.CameraMatrix );
					animator.Update( gameTime, weapon, steps );  
				}
			}
		}


		void ChangeWeaponModel( GameState gs, RenderModel model )
		{
			var rs = gs.GetService<RenderSystem>();
			var rw = rs.RenderWorld;
			var fx = gs.GetService<SFX.FXPlayback>();

			SafeDispose( ref renderModel );
			animator	=	null;

			if (model!=null)
			{
				renderModel	=	new RenderModelInstance( gs, model, rw.Camera.CameraMatrix, CAMERA_NODE );
				animator	=	new WeaponAnimator(fx, renderModel); 
			}
		}
	}
}
