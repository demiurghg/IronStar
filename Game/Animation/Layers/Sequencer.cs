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
	public class Sequencer : BaseLayer 
	{
		public float TimeScale { get; set; } = 1;

		public override bool IsPlaying 
		{ 
			get { return animations.Any(); }
		}

		TimeSpan trackTime;

		List<Animation> animations = new List<Animation>();

		AnimationKey[] keyTransforms;


		public Sequencer( Scene scene, string channel, AnimationBlendMode blendMode ) : base(scene, channel, blendMode)
		{
			trackTime		=	new TimeSpan(0);
			keyTransforms	=	new AnimationKey[ scene.Nodes.Count ];
		}


		public override bool Evaluate ( GameTime gameTime, Matrix[] destination )
		{
			//	remove played clips :
			animations.RemoveAll( anim => anim.End < trackTime );

			//	apply transofrms :
			bool r = ApplyTransforms( destination );

			//	advance time :
			trackTime += gameTime.Elapsed;

			return r;
		}


		public void Sequence( AnimationTake take, SequenceMode sequenceMode )
		{	
			Sequence( take, sequenceMode, TimeSpan.Zero );
		}


		public TimeSpan GetTakeLength( string takeName )
		{
			var take	=	scene.Takes[ takeName ];

			if (take==null) 
			{
				Log.Warning("Take '{0}' does not exist", takeName );
				return TimeSpan.Zero;
			}

			return Scene.ComputeFrameLength( take.FrameCount, scene.TimeMode );
		}

		/// <summary>
		/// Sequence new take.
		/// 
		/// Crossfade policy:
		///		- No crossfade, if no take is sequenced
		///		-
		/// </summary>
		/// <param name="take"></param>
		/// <param name="sequenceMode"></param>
		/// <param name="crossfade"></param>
		public void Sequence( AnimationTake take, SequenceMode sequenceMode, TimeSpan crossfade, TimeMode timeModeOverride = TimeMode.Unknown )
		{
			var immediate	=	sequenceMode.HasFlag( SequenceMode.Immediate );
			var looped		=	sequenceMode.HasFlag( SequenceMode.Looped );
			var hold		=	sequenceMode.HasFlag( SequenceMode.Hold );
			var noTwice		=	sequenceMode.HasFlag( SequenceMode.DontPlayTwice );
			var reverse		=	sequenceMode.HasFlag( SequenceMode.Reverse );

			//	looped and holds contradicts each other :
			if (looped && hold) throw new ArgumentException("SequenceMode.Looped and SequenceMode.Hold are incompatible");

			//	dont place the same take twice :
			if (noTwice)
			{
				if (animations.Any( anim => anim.Take == take ))
				{
					return;
				}
			}

			var lastAnim	=	animations.LastOrDefault();
			var startTime	=	(lastAnim==null || immediate) ? trackTime : lastAnim.GetTerminationTime(trackTime);

			var newAnim			=	new Animation( this, startTime, take, looped, hold, reverse, timeModeOverride );
			newAnim.Crossfade	=	(lastAnim==null) ? TimeSpan.Zero : crossfade;

			StopAllAnimationsAt( startTime + crossfade );

			animations.Add( newAnim );
		}


		/// <summary>
		/// Force all playing animations to be stopped at given time.
		/// If animation stops earlier, its stop time is not changed.
		/// </summary>
		/// <param name="stopTime"></param>
		void StopAllAnimationsAt( TimeSpan stopTime )
		{
			foreach ( var anim in animations )
			{
				if (anim.End>stopTime)
				{
					anim.End	=	stopTime;
				}
			}
		}


		public void Sequence ( string takeName, SequenceMode sequenceMode )
		{
			Sequence( takeName, sequenceMode, TimeSpan.Zero );
		}


		public void Sequence ( string takeName, SequenceMode sequenceMode, TimeSpan crossfade, TimeMode timeMode = TimeMode.Unknown )
		{
			var take	=	scene.Takes[ takeName ];

			if (take==null) 
			{
				Log.Warning("Take '{0}' does not exist", takeName );
				return;
			}

			Sequence( take, sequenceMode, crossfade, timeMode );
		}


		/*-----------------------------------------------------------------------------------------
		 *  Internal stuff :
		-----------------------------------------------------------------------------------------*/

		public bool ApplyTransforms ( Matrix[] destination )
		{
			if ( !IsPlaying || Weight==0 ) 
			{
				return false; // bypass track
			}

			var	dst	=	Matrix.Identity;
			var	src	=	AnimationKey.Identity;

			bool additive = blendMode==AnimationBlendMode.Additive;

			for ( int chIdx = 0; chIdx < channelIndices.Length; chIdx++ ) 
			{
				foreach ( var anim in animations )
				{
					if (trackTime>=anim.Start && trackTime<=anim.End)
					{
						int nodeIndex	=	channelIndices[ chIdx ];
						var weight		=	Weight;

						dst				=	destination[nodeIndex];

						if (additive) 
						{
							float crossfade = anim.Sample( nodeIndex, trackTime, ref src );
							dst = AnimationUtils.Lerp( dst, dst * src.Transform, weight * crossfade );
						} 
						else 
						{
							float crossfade = anim.Sample( nodeIndex, trackTime, ref src );
							dst = AnimationUtils.Lerp( dst, src.Transform, weight * crossfade );
						}

						destination[nodeIndex] = dst;
					}
				}
			}

			return true;
		}
	}
}
