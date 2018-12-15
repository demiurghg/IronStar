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
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion.Engine.Audio;


namespace IronStar.SFX {
	public partial class FXPlayback : DisposableBase {

		TextureAtlas spriteSheet;

		readonly Game			game;
		public readonly RenderWorld	rw;
		public readonly SoundSystem	ss;
		public readonly GameWorld world;

		List<FXInstance> runningSFXes = new List<FXInstance>();

		float timeAccumulator = 0;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public FXPlayback ( GameWorld world )
		{
			this.world	=	world;
			this.game	=	world.Game;
			this.rw		=	game.RenderSystem.RenderWorld;
			this.ss		=	game.SoundSystem;

			Game_Reloading(this, EventArgs.Empty);
			game.Reloading +=	Game_Reloading;
		}



		protected override void Dispose( bool disposing )
		{
			if (disposing) {
				StopAllSFX();
				game.Reloading -= Game_Reloading;
			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Game_Reloading ( object sender, EventArgs e )
		{
			spriteSheet	=  world.Content.Load<TextureAtlas>(@"sprites\particles");

			rw.ParticleSystem.Images	=	spriteSheet;	
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public SoundEventInstance	CreateSoundEventInstance ( string path )
		{
			if (string.IsNullOrWhiteSpace(path)) {
				return null;
			}

			try {
				var soundEvent = ss.GetEvent( path );

				return soundEvent.CreateInstance();

			} catch ( SoundException se ) {

				Log.Warning(se.ToString());
				return null;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public void StopAllSFX ()
		{
			rw.ParticleSystem.Images	=	null;
			runningSFXes.Clear();
		}



		/// <summary>
		/// Updates visible meshes
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update ( GameTime gameTime, float lerpFactor )
		{
			const float dt = 1/60.0f;
			timeAccumulator	+= gameTime.ElapsedSec;

			while ( timeAccumulator > dt ) {

				foreach ( var sfx in runningSFXes ) {

					sfx.Update( dt, lerpFactor );

					if (sfx.IsExhausted) {
						sfx.Kill();
					}
				}

				runningSFXes.RemoveAll( sfx => sfx.IsExhausted );

				timeAccumulator -= dt;				
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="imageName"></param>
		/// <returns></returns>
		public int GetSpriteIndex ( string spriteName )
		{
			var name	=	Path.GetFileName(spriteName);
			var clip	=	spriteSheet.GetClipByName( spriteName );

			if (clip==null) {
				Log.Warning("{0} not included to sprite sheet", spriteName);
				return -1;
			} else {
				return clip.FirstIndex;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxEvent"></param>
		public FXInstance RunFX ( FXEvent fxEvent, bool looped )
		{
			var fxAtomID	=	fxEvent.FXAtom;

			if (fxAtomID<0) {
				Log.Warning("RunFX: negative atom ID");
				return null;
			}

			var className = world.Atoms[ fxAtomID ];

			if (className=="*trail_bullet") {
				RunTrailBullet( fxEvent );
				return null;
			}

			if (className=="*trail_gauss") {
				RunTrailGauss( fxEvent );
				return null;
			}

			if (className=="*trail_laser") {
				Log.Warning("\"*rail_trail\" is not implemented");
				return null;
			}

			if (className==null) {
				Log.Warning("RunFX: bad atom ID");
				return null;
			}


			var factory		=	world.Content.Load<FXFactory>( Path.Combine("fx", className), (FXFactory)null );

			if (factory==null) {
				return null;
			}

			var fxInstance	=	factory.CreateFXInstance( this, fxEvent, looped );

			runningSFXes.Add( fxInstance );

			return fxInstance;
		}
	}
}
