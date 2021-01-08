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
		readonly protected Sequencer sequencer;
		readonly protected bool additive;
		public readonly bool Looped;
		public readonly bool Hold;
		public readonly bool Reverse;
		public	TimeSpan Start;
		public	TimeSpan End;
		public	TimeSpan Length;
		public	TimeSpan Crossfade;

		public bool IsInfinite { get { return End == TimeSpan.MaxValue; } }

		public AnimClip( Sequencer sequencer, TimeSpan startTime, TimeSpan length, bool looped, bool hold, bool reverse )
		{
			this.sequencer	=	sequencer;
			this.Start		=	startTime;
			this.Length		=	length;
			this.End		=	startTime + length;
			this.Looped		=	looped;
			this.Hold		=	hold;
			this.Reverse	=	reverse;

			if (looped || hold)
			{
				End	=	TimeSpan.MaxValue;
			}

			this.additive	=	sequencer.blendMode==AnimationBlendMode.Additive;
		}

		protected float GetCrossfadeFactor( TimeSpan trackTime )
		{
			if (Crossfade==TimeSpan.Zero) return 1;
			
			var localTime	=	trackTime - Start;

			if ( localTime < TimeSpan.Zero ) return 0;
			if ( localTime > Crossfade ) return 1;

			var crossfadeSeconds	=	Crossfade.TotalSeconds;
			var localTimeSeconds	=	localTime.TotalSeconds;

			return (float)MathUtil.Saturate( localTimeSeconds / crossfadeSeconds );
		}

		
		public TimeSpan GetTerminationTime( TimeSpan trackTime )
		{
			if (!Looped && !Hold) 
			{
				return End;
			}

			if (Hold) 
			{
				return AnimationUtils.Max( Start + Length, trackTime );
			}

			if (Looped) 
			{
				var localTicks	=	(trackTime - Start).Ticks;
				var lengthTicks	=	Length.Ticks;
				var loopCount	=	MathUtil.IntDivRoundUp( localTicks, lengthTicks );

				var termTime	=	Start + new TimeSpan( loopCount * lengthTicks );
			}

			Log.Warning("GetTerminationTime -- bad hold/loop state");

			return trackTime;
		}


		public abstract void Play();
		public abstract float Sample( int nodeIndex, TimeSpan trackTime, ref AnimationKey key );
	}


	class Transition : AnimClip
	{
		AnimationKey[]	originPose;
		AnimationKey[]	targetPose;
		AnimationCurve	curve;
		AnimationTake	take;

		public Transition( Sequencer sequencer, AnimationTake take, int frame, AnimationCurve curve, TimeSpan startTime, TimeSpan length, bool looped, bool hold, bool reverse )
		 : base(sequencer, startTime, length, looped, hold, reverse)
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

		public override float Sample( int nodeIndex, TimeSpan trackTime, ref AnimationKey key )
		{
			float factorTime	=	(float)((trackTime.TotalMilliseconds - Start.TotalMilliseconds) / Length.TotalMilliseconds);
			float factorCurve	=	AnimationUtils.Curve( curve, MathUtil.Saturate(factorTime) );

			if (additive) 
			{
				key = AnimationKey.Lerp( AnimationKey.Identity, targetPose[nodeIndex], factorCurve );
			} 
			else 
			{
				key = AnimationKey.Lerp( originPose[nodeIndex], targetPose[nodeIndex], factorCurve );
			}

			return 1;
		}
	}


	class Animation : AnimClip 
	{
		readonly TimeMode timeMode;
		public readonly AnimationTake Take;

		public Animation ( Sequencer sequencer, TimeSpan startTime, AnimationTake take, bool looped, bool hold, bool reverse )
		 : base( sequencer, startTime, Scene.ComputeFrameLength( take.FrameCount, sequencer.Scene.TimeMode ), looped, hold, reverse )
		{
			this.Take		=	take;
			this.timeMode	=	sequencer.Scene.TimeMode;
		}


		public override void Play()	{ /* do nothing */ }


		public override float Sample ( int node, TimeSpan trackTime, ref AnimationKey key )
		{
			int prev, next;
			float weight;
			float crossfade = GetCrossfadeFactor(trackTime);
			int frameCount = Take.FrameCount;

			Scene.TimeToFrames( trackTime - Start, timeMode, out prev, out next, out weight );

			if (Reverse)
			{
				prev	=	frameCount - MathUtil.Wrap( prev, 0, frameCount );
				next	=	frameCount - MathUtil.Wrap( next, 0, frameCount );
			}

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

			return crossfade;
		}
	}
}
