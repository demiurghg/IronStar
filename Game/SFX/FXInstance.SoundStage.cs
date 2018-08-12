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

	/// <summary>
	/// 
	/// </summary>
	public partial class FXInstance {

		
		public class SoundStage : Stage {

			SoundEventInstance soundInstance;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="instance"></param>
			/// <param name="position"></param>
			/// <param name="soundPath"></param>
			public SoundStage ( FXInstance instance, FXSoundStage stageDesc, FXEvent fxEvent, bool looped ) : base(instance)
			{
				soundInstance	=	instance.fxPlayback.CreateSoundEventInstance( stageDesc.Sound );

				if (soundInstance==null) {
					return;
				}

				soundInstance.Set3DParameters( fxEvent.Origin, fxEvent.Velocity );
				soundInstance.ReverbLevel = stageDesc.Reverb;
				soundInstance.Start();

				/*if (!looped) {	
					soundInstance.Stop(false);
				} */
			}


			public override void Stop ( bool immediate )
			{
				soundInstance?.Stop( immediate );
			}


			public override bool IsExhausted ()
			{
				if (soundInstance==null) {
					return true;
				}	
				
				return soundInstance.IsStopped;			
			}


			public override void Kill ()
			{
				soundInstance.Stop(true);
				soundInstance.Release();
			}


			public override void Update ( float dt, FXEvent fxEvent )
			{
				soundInstance.Set3DParameters( fxEvent.Origin, fxEvent.Velocity );
			}

		}

	}
}
