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


namespace IronStar.SFX {
	public class AnimTrack {

		readonly Scene sourceClip;
		readonly float frameRate;
		readonly float fadeInRate;
		readonly float fadeOutRate;

		readonly float lengthInFrames;
		readonly float weight;

		readonly Matrix[] localTransforms;

		bool looped;

		float time;

		
		public bool Stopped {
			get; private set;
		}

		
		public float Weight {
			get {
				return weight;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceClip"></param>
		public AnimTrack ( Scene sourceClip, bool looped, float fadein, float fadeout, float weight, float frameRate = 60 )
		{
			this.looped			=	looped;
			this.frameRate		=	frameRate;
			this.sourceClip		=	sourceClip;
			this.fadeInRate		=	fadein;
			this.fadeOutRate	=	fadeout;
			this.weight			=	weight;
			Stopped				=	false;

			if (fadein<0) {
				throw new ArgumentException("fadein < 0");
			}
			if (fadeout<0) {
				throw new ArgumentException("fadeout < 0");
			}

			lengthInFrames		=	sourceClip.LastTakeFrame - sourceClip.FirstTakeFrame;

			if (lengthInFrames<=0) {
				throw new InvalidOperationException("Source clip has negative or zero length");
			}

			localTransforms	=	new Matrix[ sourceClip.Nodes.Count ];
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="f_in"></param>
		/// <param name="f_out"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		float Ramp(float f_in, float f_out, float t) 
		{
			float y = 1;
			t = MathUtil.Clamp(t, 0, 1);
	
			float k_in	=	1 / f_in;
			float k_out	=	-1 / (1-f_out);
			float b_out =	-k_out;	
	
			if (t<f_in)  y = t * k_in;
			if (t>f_out) y = t * k_out + b_out;
	
			return y;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="time"></param>
		public void Evaluate ( float dt )
		{
			float frame;

			time	+=	dt;
			frame	=	time * frameRate;
			frame	+=	sourceClip.FirstTakeFrame;

			if (looped) {
				while (frame>lengthInFrames) {
					frame	-=	lengthInFrames;
				}
			} else {
				if (frame>lengthInFrames) {
					frame	=	lengthInFrames;
					Stopped	=	true;
				}
			}





			frame	+=	sourceClip.FirstTakeFrame;

			sourceClip.GetAnimSnapshot( frame, sourceClip.FirstTakeFrame, sourceClip.LastTakeFrame, AnimationMode.Clamp, localTransforms );
		}

		
		


	}
}
