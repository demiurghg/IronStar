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

			var model		=	new ModelInstance( this, modelDesc, world.Content, entity );

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



		int idle_timer;

		class AnimEvent {
			public Scene clip;
			public float frame;
			public float length;
			public float fps { get { return clip.FramesPerSecond; } }
			public float weight {
				get {
					return Math.Max(0, Math.Min(1, (1 - frame / length) * 2));
				}
			}
		}

		WeaponState oldWeaponState;
		Vector3 oldVelocity;

		List<AnimEvent> animEvents = new List<AnimEvent>();


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

			if (weaponModelInstance==null) {
				return;
			}

			//var dtime		=	(int)(elapsedTime * 1000);

			//
			//	walk / idle :
			//
			#region OLD
			//var idle_transforms	=	new Matrix[256];
			//var walk_transforms	=	new Matrix[256];
			//var transforms		=	new Matrix[256];

			//var anim_idle	=	weaponModelInstance.GetClip("anim_idle");
			//var anim_walk	=	weaponModelInstance.GetClip("anim_walk");

			//anim_idle.PlayTake( idle_timer, true, idle_transforms );
			//anim_walk.PlayTake( idle_timer, true, walk_transforms );

			//AnimBlend.Blend( idle_transforms, walk_transforms, 0, transforms );

			//idle_timer += dtime;

			////
			////	events :
			////
			//var event_transforms	=	new Matrix[256];
			//var newWeaponState		=	world.snapshotHeader.WeaponState;

			//if ( oldWeaponState != newWeaponState ) {
			//	oldWeaponState	=	newWeaponState;

			//	Log.Warning("...weapon: {0}", newWeaponState );

			//	if (newWeaponState==WeaponState.Recoil1 || newWeaponState==WeaponState.Recoil2) {
			//		var animEvent		= new AnimEvent();
			//		animEvent.clip		= weaponModelInstance.GetClip("anim_recoil");
			//		animEvent.frame		= 0;
			//		animEvent.length	= (animEvent.clip.LastTakeFrame - animEvent.clip.FirstTakeFrame);

			//		animEvents.Add( animEvent );
			//	}


			//}
			

			//foreach ( var animEvent in animEvents ) {
			//	animEvent.clip.PlayTake( animEvent.frame, false, event_transforms );
			//	animEvent.frame += elapsedTime * animEvent.fps;

			//	Log.Warning("...ae: {0} {1}", animEvent.clip.TakeName, animEvent.weight);

			//	AnimBlend.Blend( transforms, event_transforms, animEvent.weight, transforms );
			//}


			//animEvents.RemoveAll( ae => ae.frame > ae.length );
			#endregion

			var newWeaponState	=	world.snapshotHeader.WeaponState;

			if ( oldWeaponState != newWeaponState ) {
				oldWeaponState	=	newWeaponState;

				Log.Warning("...weapon: {0}", newWeaponState );

				if (newWeaponState==WeaponState.Recoil1 || newWeaponState==WeaponState.Recoil2) {
					weaponModelInstance.Animator.PlayEvent( AnimChannel.All, "anim_recoil", 0, 3 );
				}


			}

			//
			//	final transform :
			//
			var weaponMatrix	=	Matrix.Identity;
			var camMatrix		=	rw.Camera.GetCameraMatrix(Fusion.Drivers.Graphics.StereoEye.Mono);
				
			weaponModelInstance?.Update( elapsedTime, 0, weaponMatrix * camMatrix );
		}
	}
}
