using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
			resource?.Stop(false);
		}

		protected override void Process( Entity entity, GameTime gameTime, SoundEventInstance resource, SoundComponent sound, Transform transform )
		{
			resource?.Set3DParameters( transform.Position, transform.LinearVelocity );
		}


		public static void PlaySound( GameState gs, Entity entity, string soundPath )
		{
			var t = entity.GetComponent<Transform>();
			gs.Spawn( new SoundEntityFactory( soundPath, t.Position, t.LinearVelocity, entity ) );
		}
	}
}
