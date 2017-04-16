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
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion.Engine.Audio;
using BEPUphysics.BroadPhaseEntries;
using IronStar.Views;

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
			world.ForEachEntity( ent => ent.MakeRenderStateDirty() );
			weaponModelDirty = true;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="modelAtom"></param>
		/// <param name="entity"></param>
		/// <returns></returns>
		public ModelInstance AddModel ( short modelAtom, Entity entity )
		{
			var modelName	=	world.Atoms[modelAtom];

			var modelDesc	=	world.Content.Load<ModelDescriptor>( @"models\" + modelName );

			var scene		=	world.Content.Load<Scene>( modelDesc.ScenePath );

			var model		=	new ModelInstance( this, modelDesc, scene, entity );

			if (entity!=null) {
				AddModel( model );
			}

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
		public void Update ( float elapsedTime, float lerpFactor, GameCamera gameCamera, UserCommand userCmd )
		{	
			models.RemoveAll( m => m.Killed );

			foreach ( var model in models ) {
				model.Update( elapsedTime, lerpFactor );
			}


			//
			//	update view-space weapon model :
			//
			if (gameCamera!=null && userCmd!=null) {
				UpdateViewModel( elapsedTime, lerpFactor, gameCamera, userCmd );
			}
		}


		float weaponYaw = 0;
		float weaponRoll = 0;
		bool oldTraction = true;
		float landingKick = 0;
		float landingKickF = 0;


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




		/// <summary>
		/// 
		/// </summary>
		/// <param name="elapsedTime"></param>
		/// <param name="lerpFactor"></param>
		/// <param name="gameCamera"></param>
		/// <param name="userCmd"></param>
		void UpdateViewModel ( float elapsedTime, float lerpFactor, GameCamera gameCamera, UserCommand userCmd )
		{
			if (weaponModel != world.snapshotHeader.WeaponModel || weaponModelDirty) {

				weaponModelDirty	=	false;

				weaponModel	= world.snapshotHeader.WeaponModel;
				weaponModelInstance?.Kill();

				if (weaponModel>0) {
					weaponModelInstance	=	AddModel( weaponModel, null );
				} else {
					weaponModelInstance =	null;
				}
			}


			var playerEntity	=	world.GetPlayerEntity(world.UserGuid);
			var newTraction		=	(playerEntity==null) ? true : playerEntity.State.HasFlag( EntityState.HasTraction );

			landingKick	=	MathUtil.Drift( landingKick, 0, 3*elapsedTime, 3*elapsedTime );

			if (newTraction && !oldTraction) {
				landingKick = -0.1f;
				Log.Message("Landing");
			}
			oldTraction = newTraction;

			landingKickF	=	Filter( landingKickF, landingKick, 0.05f, elapsedTime );


			float maxWeaponRoll		=  MathUtil.DegreesToRadians(3);
			float maxWeaponYaw		= -MathUtil.DegreesToRadians(3);
			float angularThreshold	= MathUtil.DegreesToRadians(90) * elapsedTime;
			float angularVelocity	= MathUtil.DegreesToRadians( 5) * elapsedTime;

			if ( Math.Abs(userCmd.DYaw) > angularThreshold ) {
				
				float sign = Math.Sign( userCmd.DYaw );

				weaponYaw	= Filter( weaponYaw , sign * maxWeaponYaw , 0.1f, elapsedTime );
				weaponRoll	= Filter( weaponRoll, sign * maxWeaponRoll, 0.1f, elapsedTime );
				//weaponYaw	= MathUtil.Drift( weaponYaw , sign * maxWeaponYaw , angularVelocity, angularVelocity );
				//weaponRoll	= MathUtil.Drift( weaponRoll, sign * maxWeaponRoll, angularVelocity, angularVelocity );

			} else {

				weaponYaw	= Filter( weaponYaw , 0, 0.1f, elapsedTime );
				weaponRoll	= Filter( weaponRoll, 0, 0.1f, elapsedTime );
				//weaponYaw	= MathUtil.Drift( weaponYaw , 0, angularVelocity, angularVelocity );
				//weaponRoll	= MathUtil.Drift( weaponRoll, 0, angularVelocity, angularVelocity );

			}

			var weaponMatrix	=	Matrix.RotationYawPitchRoll( weaponYaw, 0, weaponRoll )
								*	Matrix.Translation(0, landingKickF, 0);

			var camMatrix	=	rw.Camera.GetCameraMatrix(Fusion.Drivers.Graphics.StereoEye.Mono);
				
			weaponModelInstance?.Update( elapsedTime, world.snapshotHeader.WeaponAnimFrame, weaponMatrix * camMatrix );
		}
	}
}
