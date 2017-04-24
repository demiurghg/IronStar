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
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion.Engine.Audio;
using IronStar.Views;


namespace IronStar.SFX {

	partial class Animator {

		class AnimEvent {

			public readonly Animator Animator;
			public readonly AnimChannel Channel;
			public readonly Scene Clip;
			public readonly float Length;
			public readonly float Fps;
			public readonly float FadeIn;
			public readonly float FadeOut;

			public float Frame;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="channel"></param>
			/// <param name="clip"></param>
			/// <param name="fadein"></param>
			/// <param name="fadeout"></param>
			public AnimEvent ( Animator animator, AnimChannel channel, Scene clip, float fadein, float fadeout )
			{
				this.Animator	=	animator;
				this.Channel	=	channel;
				this.Clip		=	clip;
				this.Frame		=	0;
				this.Fps		=	clip.FramesPerSecond;
				this.Length		=	clip.LastTakeFrame - clip.FirstTakeFrame;
				this.FadeIn		=	fadein;
				this.FadeOut	=	fadeout;

				if (fadein<0 || fadein>Length) {	
					throw new ArgumentOutOfRangeException( "fadein" );
				}

				if (fadeout<0 || fadeout>Length) {	
					throw new ArgumentOutOfRangeException( "fadeout" );
				}
			}


			/// <summary>
			/// 
			/// </summary>
			/// <param name="elapsedTime"></param>
			/// <param name="destination"></param>
			public void UpdateAndBlend ( float elapsedTime, Matrix[] destination )
			{
				var indices	=	Animator.GetChannelIndices( Channel );

				var weight	=	Ramp( Frame, FadeIn, FadeOut );

				Clip.PlayTakeAndBlend( Frame, false, indices, weight, destination );

				//Log.Verbose("anim event: {0} {1}", Clip.TakeName, weight );
				
				Frame += elapsedTime * Fps;
			}



			/// <summary>
			/// Indicates that event has completed
			/// </summary>
			public bool IsCompleted {
				get {
					return Frame > Length;
				}
			}



			/// <summary>
			/// 
			/// </summary>
			/// <param name="x"></param>
			/// <param name="fi"></param>
			/// <param name="fo"></param>
			/// <returns></returns>
			float Ramp ( float x, float fi, float fo )
			{
				var L	=	Length;
				var ki	=	 (1.0f / fi);
				var ko	=	-(1.0f / fo);

				var yi	=	(fi <= 0) ? 1 :  (x)/fi;
				var yo	=	(fo <= 0) ? 1 : -(x-L)/fo;
				
				return Math.Max( Math.Min( 1, Math.Min( yi, yo ) ), 0 );
			}
		}
	}
}
