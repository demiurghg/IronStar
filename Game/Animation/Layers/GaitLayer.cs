using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using Fusion.Scripting;
using KopiLua;
using IronStar.Animation;

namespace IronStar.Animation 
{
	public class GaitLayer : BaseLayer 
	{
		public override bool IsPlaying { get { return true; } }



		AnimationTake	takeIdle;
		AnimationTake	takeWalk;
		AnimationTake	takeRun;

		AnimationKey[]	pose;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name">Take name. May be null, first track will be set</param>
		/// <param name="blendMode"></param>
		public GaitLayer( Scene scene, string channel, AnimationBlendMode blendMode ) : base(scene, channel, blendMode)
		{
			takeWalk			=	scene.Takes["walk"];
			takeIdle			=	scene.Takes["idle"];
			takeRun				=	scene.Takes["run"];

			pose				=	new AnimationKey[ scene.Nodes.Count ];
		}


		public void Advance( float groundVelocity, GameTime gameTime )
		{
			//travalledDistance += groundVelocity * gameTime.ElapsedSec;
		}


		int GetWalkFrameIndex( int index )
		{
			return 3 + (index % 4);
		}

		int GetRunFrameIndex( int index )
		{
			return 7 + (index % 4);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="time"></param>
		/// <param name="destination"></param>
		public override bool Evaluate ( GameTime gameTime, Matrix[] destination )
		{
			//	apply transofrms :
			if ( Weight==0 ) 
			{
				return false; // bypass track
			}

			//Frame = MathUtil.Clamp( Frame + take.FirstFrame, take.FirstFrame, take.LastFrame );

			ApplyIdleAnimation( gameTime );

			for ( int i=0; i<nodeCount; i++ )
			{
				destination[i] = pose[i].Transform;
			}

			return true;
		}



		float idlePoseCooldownTimer = 0;
		float idlePoseTransitionTime;
		int currentIdlePose = -1;
		int nextIdlePose = 0;
		bool idleWaiting = false;

		void ApplyIdleAnimation( GameTime gameTime )
		{
			float dt = gameTime.ElapsedSec;

			if (idlePoseCooldownTimer<=0)
			{
				if (currentIdlePose!=nextIdlePose)
				{
					currentIdlePose			=	nextIdlePose;
					idlePoseTransitionTime	=	MathUtil.Random.NextFloat( 0.7f, 1.2f );
				}
				else
				{
					currentIdlePose			=	nextIdlePose;
					nextIdlePose			=	MathUtil.Random.Next( takeIdle.FrameCount );
					idlePoseTransitionTime	=	MathUtil.Random.NextFloat( 0.5f, 0.6f );
				}

				idlePoseCooldownTimer	=	idlePoseTransitionTime;
			}


			var firstFrame	=	takeIdle.FirstFrame;
			var factor		=	1.0f - MathUtil.Clamp( idlePoseCooldownTimer / idlePoseTransitionTime, 0, 1 );
				factor		=	AnimationUtils.SlowFollowThrough(factor);
			idlePoseCooldownTimer -= dt;

			for ( int nodeIndex=0; nodeIndex<nodeCount; nodeIndex++ )
			{
				AnimationKey k0, k1;
				takeIdle.GetKey( firstFrame + currentIdlePose, nodeIndex, out k0 ); 
				takeIdle.GetKey( firstFrame + nextIdlePose, nodeIndex, out k1 ); 

				var k = AnimationKey.Lerp( k0, k1, factor );

				pose[nodeIndex]	=	k;
			}
		}



		void ApplyLocomotionAnimation( float travelledDistance )
		{
			/*float frame		=	travalledDistance / 1.7f * 0.32f * 2;
			float factor	=	frame % 1.0f;
			int frame0		=	(int)Math.Floor( frame );
			int frame1		=	frame0 + 1;			*/

		}

	}
}
