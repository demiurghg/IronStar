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
	public class TakeSequencer : BaseLayer 
	{
		public float TimeScale { get; set; } = 1;

		public override bool IsPlaying 
		{ 
			get { return currentAnim!=null; }
		}

		TimeSpan trackTime;
		Animation currentAnim;
		Animation pendingAnim;

		AnimationKey[] keyTransforms;


		public TakeSequencer( Scene scene, string channel, AnimationBlendMode blendMode ) : base(scene, channel, blendMode)
		{
			trackTime		=	new TimeSpan(0);
			keyTransforms	=	new AnimationKey[ scene.Nodes.Count ];
		}


		public override bool Evaluate ( GameTime gameTime, Matrix[] destination )
		{
			//	update sequence twice 
			//	to avoid stucking 
			//	on extremely short clips.
			UpdateSequence();
			UpdateSequence();

			//	apply transofrms :
			bool r = ApplyTransforms( destination );

			//	advance time :
			trackTime += gameTime.Elapsed;

			return r;
		}


		public void Sequence( AnimationTake take, SequenceMode sequenceMode )
		{
			var immediate	=	sequenceMode.HasFlag( SequenceMode.Immediate );
			var looped		=	sequenceMode.HasFlag( SequenceMode.Looped );
			var hold		=	sequenceMode.HasFlag( SequenceMode.Hold );
			var noTwice		=	sequenceMode.HasFlag( SequenceMode.DontPlayTwice );

			//	looped and holds contradicts each other :
			if (looped && hold) throw new ArgumentException("SequenceMode.Looped and SequenceMode.Hold are incompatible");

			//	dont place the same take twice
			if (noTwice)
			{
				if (pendingAnim!=null && pendingAnim.Take == take) 
				{
					return;
				}
			}

			//	sequence take :
			if (currentAnim==null || immediate) 
			{
				var anim	= new Animation ( this, trackTime, take, looped, hold );
	
				currentAnim	=	anim;
				pendingAnim	=	null;
				currentAnim.Play();
			} 
			else 
			{
				var anim	=	new Animation ( this, currentAnim.End, take, looped, hold );
				pendingAnim	=	anim;
			}
		}


		public void Sequence ( string takeName, SequenceMode sequenceMode )
		{
			var take	=	scene.Takes[ takeName ];

			if (take==null) 
			{
				Log.Warning("Take '{0}' does not exist", takeName );
				return;
			}

			Sequence( take, sequenceMode );
		}


		/*-----------------------------------------------------------------------------------------
		 *  Internal stuff :
		-----------------------------------------------------------------------------------------*/

		public void ApplyTransforms ( AnimationKey[] keys )
		{
			var key = AnimationKey.Identity;

			for (int nodeIndex=0; nodeIndex < nodeCount; nodeIndex++)
			{
				currentAnim.Sample( nodeIndex, trackTime, ref key );
				keys[ nodeIndex ] = key;
			}
		}

		public bool ApplyTransforms ( Matrix[] destination )
		{
			if ( currentAnim==null || Weight==0 ) 
			{
				return false; // bypass track
			}

			var	dst	=	Matrix.Identity;
			var	src	=	AnimationKey.Identity;

			bool additive = blendMode==AnimationBlendMode.Additive;


			for ( int chIdx = 0; chIdx < channelIndices.Length; chIdx++ ) 
			{
				
				int nodeIndex	=	channelIndices[ chIdx ];
				var weight		=	Weight;

				dst				=	destination[nodeIndex];

				if (additive) 
				{
					currentAnim.Sample( nodeIndex, trackTime, ref src );
					dst = AnimationUtils.Lerp( dst, dst * src.Transform, weight );
				} 
				else 
				{
					currentAnim.Sample( nodeIndex, trackTime, ref src );
					dst = AnimationUtils.Lerp( dst, src.Transform, weight );
				}

				destination[nodeIndex] = dst;
			}

			return true;
		}


		void UpdateSequence ()
		{
			if (currentAnim!=null) 
			{
				int frameCount = currentAnim.Take.FrameCount;
			
				if ( trackTime >= currentAnim.End ) 
				{
					if ( pendingAnim!=null ) 
					{
						currentAnim			=	pendingAnim;
						currentAnim.Start	=	trackTime;
						pendingAnim = null;
					} 
					else 
					{
						if ( !currentAnim.Looped && !currentAnim.Hold ) 
						{
							currentAnim = null;
						}
					}
				}
			}
		}
	}
}
