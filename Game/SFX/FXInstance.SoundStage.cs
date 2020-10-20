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
using IronStar.Mathematics;


namespace IronStar.SFX {

	/// <summary>
	/// 
	/// </summary>
	public partial class FXInstance {

		
		public class SoundStage : Stage {

			SoundEventInstance soundInstance;
			readonly float overallScale;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="instance"></param>
			/// <param name="position"></param>
			/// <param name="soundPath"></param>
			public SoundStage ( FXInstance instance, FXSoundStage stageDesc, FXEvent fxEvent, bool looped ) : base(instance)
			{
				soundInstance	=	instance.fxPlayback.CreateSoundEventInstance( stageDesc.Sound );
				overallScale	=	instance.overallScale;

				if (soundInstance==null) {
					return;
				}

				var position	=	fxEvent.Origin;
				var velocity	=	fxEvent.Velocity;
				bool playSound	=	true;

				if (stageDesc.FlybySound)
				{
					position	=	GetFlybyPosition( instance, fxEvent, out playSound );
					velocity	=	Vector3.Zero;
				}

				if (playSound)
				{
					soundInstance.Set3DParameters( position, velocity );
					soundInstance.ReverbLevel = stageDesc.Reverb;
					soundInstance.Start();
				}

				/*if (!looped) {	
					soundInstance.Stop(false);
				} */
			}


			Vector3 GetFlybyPosition( FXInstance instance, FXEvent fxEvent, out bool playSound )
			{
				var a	=	fxEvent.Origin;
				var b	=	a + fxEvent.Velocity;
				var l	=	Vector3.Distance( a, b );
				var c	=	instance.fxPlayback.Game.SoundSystem.ListenerPosition;
				float d, t;

				Intersection.DistancePointToLineSegment( a, b, c, out d, out t );

				var d0	=	Vector3.Distance( a, c ) * 0.9f;
				var d1	=	Vector3.Distance( b, c ) * 0.9f;
				
				//	do not play whoosh sound too close 
				//	to the origin of the ray to not confuse shooter :
				playSound	=	( t * l ) > 5;

				// push whoosh forward
				//t = MathUtil.Lerp( 0.1f, 1, t );

				return Vector3.Lerp( a, b, t );
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
