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

		class AnimLoop {

			public AnimLoop ( AnimChannel channel, Scene clip, float fadein, float fadeout )
			{
				this.Channel	=	channel;
				this.Clip		=	clip;
				this.Frame		=	0;
				this.Fps		=	clip.FramesPerSecond;
				this.Length		=	clip.LastTakeFrame - clip.FirstTakeFrame;
			}

			public readonly AnimChannel Channel;
			public readonly Scene Clip;
			public readonly float Frame;
			public readonly float Length;
			public readonly float Fps;
		}
	}
}
