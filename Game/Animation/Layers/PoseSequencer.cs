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
using IronStar.Animation.Layers;

namespace IronStar.Animation 
{
	public class PoseSequencer : BaseLayer 
	{
		public override bool IsPlaying 
		{ 
			get { return currentAnim!=null; }
		}

		Pose originPose;
		Pose targetPose;

		bool			transitionActive;
		TimeSpan		time;
		TimeSpan		transitionTime;
		AnimationCurve	transitionCurve;


		public PoseSequencer( Scene scene, string channel, AnimationBlendMode blendMode ) : base(scene, channel, blendMode)
		{
			time				=	new TimeSpan(0);
			originPose			=	new Pose( scene, blendMode );
			targetPose			=	new Pose( scene, blendMode );
			transitionActive	=	false;
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
			time += gameTime.Elapsed;

			return r;
		}


		public void Sequence( AnimationTake take, int frame, TimeSpan transitionTime, AnimationCurve transitionCurve, bool immediate )
		{
			//	sequence take :
			if (currentAnim==null || immediate) 
			{
				var anim	= new Animation ( trackTime, take, scene.TimeMode, looped, hold );
	
				currentAnim	=	anim;
				pendingAnim	=	null;
			} 
			else 
			{
				var anim	=	new Animation ( currentAnim.End, take, scene.TimeMode, looped, hold );
				pendingAnim	=	anim;
			}
		}


		/*-----------------------------------------------------------------------------------------
		 *  Internal stuff :
		-----------------------------------------------------------------------------------------*/

		bool ApplyTransforms ( Matrix[] destination )
		{
			if ( currentAnim==null || Weight==0 ) {
				return false; // bypass track
			}

			bool additive = blendMode==AnimationBlendMode.Additive;


			for ( int chIdx = 0; chIdx < channelIndices.Length; chIdx++ ) 
			{
				
				int nodeIndex	= channelIndices[ chIdx ];
				var weight		= Weight;

				Matrix dst		=	destination[nodeIndex];
				Matrix src;

				if (additive) 
				{
					currentAnim.GetKey( nodeIndex, trackTime, true, out src );
					dst = AnimationUtils.Lerp( dst, dst * src, weight );
				} 
				else 
				{
					currentAnim.GetKey( nodeIndex, trackTime, false, out src );
					dst = AnimationUtils.Lerp( dst, src, weight );
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
