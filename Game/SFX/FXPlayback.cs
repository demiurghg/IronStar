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
using Fusion.Engine.Audio;
using IronStar.ECS;
using IronStar.Gameplay.Components;

namespace IronStar.SFX {
	public partial class FXPlayback : ProcessingSystem<FXInstance, FXComponent, Transform>
	{
		TextureAtlas spriteSheet;

		readonly TextureAtlasClip	EmptyClip	=	new TextureAtlasClip("*null", -1, 0 );

		public Game Game { get {  return game; } }

		readonly Game	game;
		public readonly RenderWorld	rw;
		public readonly SoundSystem	ss;
		public readonly ContentManager content;
		readonly static Random rand = new Random();

		List<FXInstance> runningSFXes = new List<FXInstance>();

		float timeAccumulator = 0;


		public FXPlayback ( Game game, ContentManager content )
		{
			this.content	=	content;
			this.game		=	game;
			this.rw			=	game.RenderSystem.RenderWorld;
			this.ss			=	game.SoundSystem;

			Game_Reloading(this, EventArgs.Empty);
			game.Reloading +=	Game_Reloading;
		}


		public ECS.Aspect GetAspect()
		{
			return ECS.Aspect.Empty;
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing) {
				StopAllSFX();
				game.Reloading -= Game_Reloading;
			}
			base.Dispose( disposing );
		}


		public void Add( GameState gs, ECS.Entity e ) {}
		public void Remove( GameState gs, ECS.Entity e ) {}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Game_Reloading ( object sender, EventArgs e )
		{
			spriteSheet	=  content.Load<TextureAtlas>(@"sprites\particles");

			rw.ParticleSystem.Images	=	spriteSheet;	
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public SoundEventInstance	CreateSoundEventInstance ( string path )
		{
			if (string.IsNullOrWhiteSpace(path)) 
			{
				return null;
			}

			try 
			{
				var soundEvent = ss.GetEvent( path );
				return soundEvent.CreateInstance();
			}
			catch ( SoundException se ) 
			{
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
		public void Update ( GameTime gameTime )
		{
			const float dt = 1/60.0f;
			timeAccumulator	+= gameTime.ElapsedSec;

			while ( timeAccumulator > dt ) 
			{
				foreach ( var sfx in runningSFXes ) 
				{
					sfx.Update( dt );

					if (sfx.IsExhausted) 
					{
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
		public TextureAtlasClip GetSpriteClip ( string spriteName )
		{
			var name	=	Path.GetFileName(spriteName);
			var clip	=	spriteSheet.GetClipByName( spriteName );

			if (clip==null) 
			{
				Log.Warning("{0} not included to sprite sheet", spriteName);
				return EmptyClip;
			}

			return clip;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxEvent"></param>
		public FXInstance RunFX ( FXEvent fxEvent, bool looped, bool attached )
		{
			var className = fxEvent.FXName;

			if (className=="*trail_bullet") 
			{
				return RunTrailBullet( fxEvent );
			}

			if (className=="*trail_gauss") 
			{
				return RunTrailGauss( fxEvent );
			}

			if (className=="*trail_laser") 
			{
				Log.Warning("\"*rail_trail\" is not implemented");
				return null;
			}

			if (className==null) 
			{
				Log.Warning("RunFX: bad atom ID");
				return null;
			}


			var factory		=	content.Load( Path.Combine("fx", className), (FXFactory)null );

			if (factory==null) 
			{
				return null;
			}

			var fxInstance	=	factory.CreateFXInstance( this, fxEvent, looped, attached );

			runningSFXes.Add( fxInstance );

			return fxInstance;
		}


		/*-----------------------------------------------------------------------------------------
		 *	ECS stuff :
		-----------------------------------------------------------------------------------------*/

		protected override FXInstance Create( ECS.Entity entity, FXComponent fx, Transform t )
		{
			var fxEvent			=	new FXEvent();
			fxEvent.FXName		=	fx.FXName;
			fxEvent.Origin		=	t.Position;
			fxEvent.Rotation	=	t.Rotation;
			fxEvent.Scale		=	(t.Scaling.X + t.Scaling.Y + t.Scaling.Z) / 3.0f;

			var velocity		=	entity.GetComponent<Velocity>();

			if (velocity!=null)
			{
				fxEvent.Velocity = velocity.Linear;
			}

			var attached	=	entity.ContainsComponent<AttachmentComponent>();

			return RunFX( fxEvent, fx.Looped, attached ); 
		}

		protected override void Destroy( ECS.Entity entity, FXInstance fxInstance )
		{
			fxInstance?.Kill();
		}

		public override void Update( GameState gs, GameTime gameTime )
		{
			base.Update( gs, gameTime );

			Update( gameTime );
		}

		protected override void Process( ECS.Entity entity, GameTime gameTime, FXInstance fxInstance, FXComponent fx, Transform t )
		{
			if ( fxInstance!=null )
			{
				fxInstance.fxEvent.Origin   =   t.Position;
				fxInstance.fxEvent.Rotation =   t.Rotation;
				fxInstance.fxEvent.Scale    =   ( t.Scaling.X + t.Scaling.Y + t.Scaling.Z ) / 3.0f;
				var velocityComponent       =   entity.GetComponent<Velocity>();
				fxInstance.fxEvent.Velocity =   velocityComponent==null ? Vector3.Zero : velocityComponent.Linear;

				if ( fxInstance.IsExhausted )
				{
					entity.gs.Kill( entity );
				}
			}
			else
			{
				//	trails creates null FX-instances, 
				//	so just delete entity:
				entity.gs.Kill( entity );
			}
		

			//	#TODO #FX -- kill exhausted FXs
			//fxInstance.Update( gameTime.ElapsedSec ); 
		}
	}
}
