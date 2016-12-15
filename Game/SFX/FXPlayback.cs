﻿using System;
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
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion.Engine.Audio;
using IronStar.Views;


namespace IronStar.SFX {
	public class FXPlayback {

		TextureAtlas spriteSheet;

		readonly Game			game;
		public readonly ShooterClient	client;
		public readonly RenderWorld	rw;
		public readonly SoundWorld	sw;
		public readonly GameWorld world;

		List<FXInstance> runningSFXes = new List<FXInstance>();

		float timeAccumulator = 0;

		Dictionary<string,Type> sfxDict = new Dictionary<string,Type>();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public FXPlayback ( ShooterClient client, GameWorld world )
		{
			this.world	=	world;
			this.client	=	client;
			this.game	=	client.Game;
			this.rw		=	game.RenderSystem.RenderWorld;
			this.sw		=	game.SoundSystem.SoundWorld;

			Game_Reloading(this, EventArgs.Empty);
			game.Reloading +=	Game_Reloading;

			FXInstance.EnumerateSFX( type => sfxDict.Add( type.Name, type ) );
		}



		/// <summary>
		/// 
		/// </summary>
		public void Shutdown ()
		{
			StopAllSFX();
			game.Reloading -= Game_Reloading;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Game_Reloading ( object sender, EventArgs e )
		{
			spriteSheet	=  client.Content.Load<TextureAtlas>(@"sprites\particles|srgb");

			rw.ParticleSystem.Images	=	spriteSheet;	
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public SoundEffect	LoadSound ( string path )
		{
			return client.Content.Load<SoundEffect>( path, (SoundEffect)null );
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
		public void Update ( float elapsedTime, float lerpFactor )
		{
			const float dt = 1/60.0f;
			timeAccumulator	+= elapsedTime;

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
			return spriteSheet.IndexOf( spriteName );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxEvent"></param>
		public FXInstance RunFX ( FXEvent fxEvent )
		{
			var fxAtomID	=	fxEvent.FXAtom;

			if (fxAtomID<0) {
				Log.Warning("RunFX: negative atom ID");
				return null;
			}

			var className = client.Atoms[ fxAtomID ];

			if (className==null) {
				Log.Warning("RunFX: bad atom ID");
				return null;
			}


			Type fxType;

			if (sfxDict.TryGetValue( className, out fxType )) {
				
				var sfx = (FXInstance)Activator.CreateInstance( fxType, this, fxEvent );
				runningSFXes.Add( sfx );

				return sfx;

			} else {
				Log.Warning("RunFX: Bad FX type name: {0}", className );
				return null;
			}
		}
	}
}
