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
	abstract class AnimClip
	{
		readonly protected TakeSequencer sequencer;
		readonly protected bool additive;
		public readonly bool Looped;
		public readonly bool Hold;
		public			TimeSpan Start;
		public readonly TimeSpan Length;
		public TimeSpan End { get { return Start + Length; } }

		public AnimClip( TakeSequencer sequencer, TimeSpan startTime, TimeSpan length, bool looped, bool hold )
		{
			this.sequencer	=	sequencer;
			this.Start		=	startTime;
			this.Length		=	length;
			this.Looped		=	looped;
			this.Hold		=	hold;
			this.Length		=	length;
			this.additive	=	sequencer.blendMode==AnimationBlendMode.Additive;
		}

		public abstract void Play();
		public abstract void Sample( int nodeIndex, TimeSpan time, ref AnimationKey key );
	}


	class Transition : AnimClip
	{
		AnimationKey[]	originPose;
		AnimationKey[]	targetPose;
		AnimationCurve	curve;
		AnimationTake	take;

		public Transition( TakeSequencer sequencer, AnimationTake take, int frame, AnimationCurve curve, TimeSpan startTime, TimeSpan length, bool looped, bool hold )
		 : base(sequencer, startTime, length, looped, hold)
		{
			this.take	=	take;
			this.curve	=	curve;
			targetPose	=	new AnimationKey[ sequencer.Scene.Nodes.Count ];
			originPose	=	new AnimationKey[ sequencer.Scene.Nodes.Count ];

			frame		=	MathUtil.Clamp( frame + take.FirstFrame, take.FirstFrame, take.FirstFrame + take.FrameCount );

			take.GetPose( frame, sequencer.blendMode, targetPose );
		}

		public override void Play()
		{
			
		}

		public override void Sample( int nodeIndex, TimeSpan time, ref AnimationKey key )
		{
			float factorTime	=	(float)((time.TotalMilliseconds - Start.TotalMilliseconds) / Length.TotalMilliseconds);
			float factorCurve	=	AnimationUtils.Curve( curve, MathUtil.Saturate(factorTime) );

			if (additive) 
			{
				key = AnimationKey.Lerp( AnimationKey.Identity, targetPose[nodeIndex], factorCurve );
			} 
			else 
			{
				key = AnimationKey.Lerp( originPose[nodeIndex], targetPose[nodeIndex], factorCurve );
			}
		}
	}


	class Animation : AnimClip 
	{
		readonly TimeMode timeMode;
		public readonly AnimationTake Take;

		public Animation ( TakeSequencer sequencer, TimeSpan startTime, AnimationTake take, bool looped, bool hold )
		 : base( sequencer, startTime, Scene.ComputeFrameLength( take.FrameCount, sequencer.Scene.TimeMode ), looped, hold )
		{
			this.Take		=	take;
			this.timeMode	=	sequencer.Scene.TimeMode;
		}


		public override void Play()	{ /* do nothing */ }


		public override void Sample ( int node, TimeSpan time, ref AnimationKey key )
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
				
			var prevT = AnimationKey.Identity;
			var nextT = AnimationKey.Identity;

			if (additive) 
			{
				Take.GetDeltaKey( prev, node, out prevT );
				Take.GetDeltaKey( next, node, out nextT );
			} 
			else 
			{
				Take.GetKey( prev, node, ref prevT );
				Take.GetKey( next, node, ref nextT );
			}

			key = AnimationKey.Lerp( prevT, nextT, weight );
		}
	}
}
