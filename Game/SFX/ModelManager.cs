using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion.Engine.Audio;
using BEPUphysics.BroadPhaseEntries;
using IronStar.Views;
using IronStar.Items;

namespace IronStar.SFX {
	public class ModelManager : DisposableBase {

		LinkedList<ModelInstance> models;

		readonly Game			game;
		public readonly RenderSystem rs;
		public readonly RenderWorld	rw;
		public readonly SoundWorld	sw;
		public readonly GameWorld world;

		public Matrix ViewMatrix;
		public Matrix ProjectionMatrix;


		short			weaponModel = 0;
		bool			weaponModelDirty = true;
		ModelInstance	weaponModelInstance = null;



		public ModelManager ( GameWorld world )
		{
			this.world	=	world;
			this.game	=	world.Game;
			this.rs		=	game.RenderSystem;
			this.rw		=	game.RenderSystem.RenderWorld;
			this.sw		=	game.SoundSystem.SoundWorld;

			Game_Reloading(this, EventArgs.Empty);
			game.Reloading +=	Game_Reloading;

			models	=	new LinkedList<ModelInstance>();
		}



		/// <summary>
		/// 
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {
				KillAllModels();
				game.Reloading -= Game_Reloading;
			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Game_Reloading( object sender, EventArgs e )
		{
			#warning RELOAD ENTITY MODELS!
			//world.ForEachEntity( ent => ent.MakeRenderStateDirty() );
			weaponModelDirty = true;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="modelAtom"></param>
		/// <param name="entity"></param>
		/// <returns></returns>
		public ModelInstance AddModel ( Entity entity, short modelAtom, bool fpv )
		{
			var modelName	=	world.Atoms[ modelAtom ];

			if (string.IsNullOrWhiteSpace(modelName)) {
				return null;
			}

			var modelDesc	=	world.Content.Load<ModelFactory>( @"models\" + modelName );
			var model		=	new ModelInstance( entity, this, modelDesc, world.Content );

			AddModel( model );

			return model;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="modelInstance"></param>
		public void AddModel ( ModelInstance modelInstance )
		{
			models.AddLast( modelInstance );
		}



		/// <summary>
		/// 
		/// </summary>
		public void KillAllModels ()
		{
			foreach ( var model in models ) {
				model.Kill();
			}
			models.Clear();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="elapsedTime"></param>
		/// <param name="lerpFactor"></param>
		public void Update ( GameTime gameTime, float lerpFactor, GameCamera gameCamera, UserCommand userCmd )
		{	
			models.RemoveAll( m => m.Killed );

			foreach ( var model in models ) {
				model.Update( gameTime.ElapsedSec, lerpFactor );
			}

			//
			//	update view-space weapon model :
			//
			if (gameCamera!=null && userCmd!=null) {
				UpdateViewModel( gameTime.ElapsedSec, lerpFactor, gameCamera, userCmd );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="current"></param>
		/// <param name="filter">Factor at 60 fps</param>
		/// <param name="factor"></param>
		/// <param name="dt"></param>
		/// <returns></returns>
		float Filter ( float current, float target, float factor, float dt )
		{
			float  factor2	=	(float)( 1 - Math.Pow( 1 - factor, dt * 60 ) );
			return MathUtil.Lerp( current, target, factor2 );
		}



		Vector3 oldVelocity;
		bool oldTraction;

		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="elapsedTime"></param>
		/// <param name="lerpFactor"></param>
		/// <param name="gameCamera"></param>
		/// <param name="userCmd"></param>
		void UpdateViewModel ( float elapsedTime, float lerpFactor, GameCamera gameCamera, UserCommand userCmd )
		{
			/*
			if (weaponModel != world.snapshotHeader.WeaponModel || weaponModelDirty) {

				weaponModelDirty	=	false;

				weaponModel	= world.snapshotHeader.WeaponModel;
				weaponModelInstance?.Kill();

				if (weaponModel>0) {
					weaponModelInstance	=	AddModel( weaponModel, null );
					weaponModelInstance.Animator.PlayLoop( AnimChannel.All, "anim_idle" );
				} else {
					weaponModelInstance =	null;
				}
			}

			if (weaponModelInstance==null) {
				return;
			}


			var player			=	world.GetPlayerEntity( world.UserGuid );
			var newWeaponState	=	world.snapshotHeader.WeaponState;

			if (player!=null) {
				var newVelocity	=	player.LinearVelocity;

				float weight	=	MathUtil.Clamp( Math.Abs(newVelocity.Y - oldVelocity.Y) / 20, 0, 1 );

				var newTraction	=	player.State.HasFlag(EntityState.HasTraction);

				if (oldTraction) {
					weight /= 2;
				}

				if (newWeaponState!=WeaponState.Idle) {
					weight = 0;
				}

				if (weight>0.1f && newTraction!=oldTraction) {
					Log.Message("Landing: {0} {1}", newVelocity.Y, oldVelocity.Y);
					weaponModelInstance.Animator.PlayEvent( AnimChannel.All, "anim_landing", weight, 0, 7 );
				}

				oldTraction	=	newTraction;
				oldVelocity =	newVelocity;
			}



			if ( oldWeaponState != newWeaponState ) {
				oldWeaponState	=	newWeaponState;

				Log.Warning("...weapon: {0}", newWeaponState );

				if (newWeaponState==WeaponState.Recoil1 || newWeaponState==WeaponState.Recoil2) {
					weaponModelInstance.Animator.PlayEvent( AnimChannel.All, "anim_recoil", 1, 1, 5 );
				}
				if (newWeaponState==WeaponState.Activating) {
					weaponModelInstance.Animator.PlayEvent( AnimChannel.All, "anim_takeout", 1, 0, 0 );
				}
				if (newWeaponState==WeaponState.Deactivating) {
					weaponModelInstance.Animator.PlayEvent( AnimChannel.All, "anim_putdown", 1, 0, 0 );
				}
				if (newWeaponState==WeaponState.Warmup) {
					weaponModelInstance.Animator.PlayEvent( AnimChannel.All, "anim_warmup", 1, 0, 0 );
				}
			}

			//
			//	final transform :
			//
			var weaponMatrix	=	Matrix.Identity;
			var camMatrix		=	rw.Camera.GetCameraMatrix(Fusion.Drivers.Graphics.StereoEye.Mono);
				
			weaponModelInstance?.Update( elapsedTime, 0, weaponMatrix * camMatrix );
			*/
		}
	}
}
