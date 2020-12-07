using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Scripting;
using Fusion.Engine.Graphics.Scenes;
using KopiLua;
using IronStar.Animation;

namespace IronStar.Animation
{
	class Animation 
	{
		readonly TimeMode timeMode;
		public readonly AnimationTake Take;
		public readonly bool Looped;
		public readonly bool Hold;
		public			TimeSpan Start;
		public readonly TimeSpan Length;
		public TimeSpan End { get { return Start + Length; } }

		public Animation ( TimeSpan startTime, AnimationTake take, TimeMode timeMode, bool looped, bool hold ) 
		{
			this.timeMode	=	timeMode;
			this.Start		=	startTime;
			this.Take		=	take;
			this.Looped		=	looped;
			this.Hold		=	hold;
			this.Length		=	Scene.ComputeFrameLength( take.FrameCount, timeMode );
		}


		public void GetKey ( int node, TimeSpan time, bool useDelta, out Matrix transform )
		{
			int prev, next;
			float weight;

			Scene.TimeToFrames( time - Start, timeMode, out prev, out next, out weight );

			if (Looped) 
			{
				prev = MathUtil.Wrap( prev + Take.FirstFrame, Take.FirstFrame, Take.LastFrame );
				next = MathUtil.Wrap( next + Take.FirstFrame, Take.FirstFrame, Take.LastFrame );
			} 
			else 
			{
				prev = MathUtil.Clamp( prev + Take.FirstFrame, Take.FirstFrame, Take.LastFrame );
				next = MathUtil.Clamp( next + Take.FirstFrame, Take.FirstFrame, Take.LastFrame );
			}
				
			AnimationKey prevT, nextT;

			if (useDelta) 
			{
				Take.GetDeltaKey( prev, node, out prevT );
				Take.GetDeltaKey( next, node, out nextT );
			} 
			else 
			{
				Take.GetKey( prev, node, out prevT );
				Take.GetKey( next, node, out nextT );
			}

			transform = AnimationKey.Lerp( prevT, nextT, weight ).Transform;
		}
	}
}
