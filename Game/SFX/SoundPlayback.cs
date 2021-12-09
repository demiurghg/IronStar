using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Engine.Audio;
using IronStar.ECS;

namespace IronStar.SFX
{
	public class SoundPlayback : ProcessingSystem<SoundEventInstance, SoundComponent, Transform>
	{
		readonly FXPlayback fxplayback;

		public SoundPlayback( FXPlayback fxplayback )
		{
			this.fxplayback	=	fxplayback;
		}

		protected override SoundEventInstance Create( Entity entity, SoundComponent sound, Transform transform )
		{
			Log.Debug("Play Sound : {0} {1}", sound.SoundName, sound.Looped ? "Looped" : "Once" );
			var soundEventInstance = fxplayback.CreateSoundEventInstance( sound.SoundName );

			if (soundEventInstance!=null)
			{
				soundEventInstance.Set3DParameters( transform.Position, transform.LinearVelocity );
				soundEventInstance.Start();
			}

			return soundEventInstance;
		}

		protected override void Destroy( Entity entity, SoundEventInstance resource )
		{
			Log.Debug("Stop Sound : {0}", resource.ToString() );
			resource?.Stop(false);
		}

		protected override void Process( Entity entity, GameTime gameTime, SoundEventInstance resource, SoundComponent sound, Transform transform )
		{
			resource?.Set3DParameters( transform.Position, transform.LinearVelocity );

			if (entity.IsLocalDomain && !sound.Looped && resource.IsStopped)
			{
				entity.Kill();
			}
		}


		public static void PlaySound( Entity entity, string soundPath )
		{
			if (entity!=null)
			{
				var transform = entity.GetComponent<Transform>();
				if (transform!=null)
				{
					entity.gs.Spawn( new SoundEntityFactory( soundPath, transform.Position, transform.LinearVelocity, entity ) );
				}
			}
		}
	}
}
