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
using IronStar.Gameplay.Components;

namespace IronStar.Animation 
{
	public partial class GaitLayer : BaseLayer 
	{
		enum AnimState
		{
			IdleHoldPose,
			IdleChangePose,
			RightStep0,
			RightStep1,
			LeftStep0,
			LeftStep1,
		}

		public override bool IsPlaying { get { return true; } }



		AnimationTake	takeIdle;
		AnimationTake	takeWalk;
		AnimationTake	takeRun;

		AnimationKey[]	originPose;
		AnimationKey[]	currentPose;
		AnimationKey[]	targetPose;

		AnimState		state			=	AnimState.IdleHoldPose;
		float			stateTimer		=	0;
		float			statePeriod		=	0.1f;
		AnimationCurve	curve			=	AnimationCurve.LinearStep;
		int				idlePose		=	0;
		int				idlePoseCount;

		bool			isWalking;
		Vector3			gorundVelocity;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name">Take name. May be null, first track will be set</param>
		/// <param name="blendMode"></param>
		public GaitLayer( Scene scene, string channel, AnimationBlendMode blendMode ) : base(scene, channel, blendMode)
		{
			takeWalk	=	scene.Takes["walk"];
			takeIdle	=	scene.Takes["idle"];
			takeRun		=	scene.Takes["run"];

			idlePoseCount	=	takeIdle.FrameCount;

			originPose	=	CreatePose();
			currentPose	=	CreatePose();
			targetPose	=	CreatePose();
		}


		public void UpdateMonsterState ( StepComponent step )
		{
			isWalking		=	step.IsWalkingOrRunning;
			gorundVelocity	=	step.GroundVelocity;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="time"></param>
		/// <param name="destination"></param>
		public override bool Evaluate ( GameTime gameTime, Matrix[] destination )
		{
			//	apply transforms :
			if ( Weight==0 ) 
			{
				return false; // bypass track
			}

			//Frame = MathUtil.Clamp( Frame + take.FirstFrame, take.FirstFrame, take.LastFrame );
			bool	timeout	=	stateTimer > statePeriod;
			float	factor	=	MathUtil.Saturate( stateTimer / statePeriod );

			stateTimer	+=	gameTime.ElapsedSec;

			AdvanceStateMachine( timeout, factor, isWalking );
			BlendPoses( factor );

			//ApplyIdleAnimation( gameTime );

			for ( int i=0; i<nodeCount; i++ )
			{
				destination[i] = currentPose[i].Transform;
			}

			return true;
		}


		void AdvanceStateMachine( bool timeout, float factor, bool locomotion )
		{
			//	The average runner will have a cadence of 150 to 170 SPM (Steps Per Minute), 
			//	while the fastest long-distance runners are up in the 180 to 200 SPM range.
			float halfStepTime	=	60.0f / 160.0f * 0.5f;

			float walkRunFactor	=	MathUtil.Saturate( (gorundVelocity.Length() / 20.0f - 0.2f) );

			switch (state)
			{
				case AnimState.IdleHoldPose:
					GenerateTargetPose( takeIdle, idlePose );
					if (timeout)
					{
						ChangeState( AnimState.IdleChangePose, 0.7f, 1.2f );
						idlePose	=	MathUtil.Random.Next( idlePoseCount );
						curve		=	MathUtil.Random.SelectRandom( AnimationCurve.SmoothStep, AnimationCurve.SmootherStep, AnimationCurve.SlowFollowThrough );
					} 
					else if (locomotion)
					{
						ChangeState( AnimState.RightStep0, halfStepTime );
						curve	=	AnimationCurve.LinearStep;
					}
				break;

				case AnimState.IdleChangePose:
					GenerateTargetPose( takeIdle, idlePose );
					if (timeout)
					{
						ChangeState( AnimState.IdleHoldPose, 0.5f, 0.6f );
					} 
					else if (locomotion)
					{
						ChangeState( AnimState.RightStep0, halfStepTime );
						curve	=	AnimationCurve.LinearStep;
					}
				break;

				case AnimState.RightStep0:
					GenerateTargetPose( takeWalk, 0, takeRun, 0, walkRunFactor );
					if (timeout)
					{
						ChangeState( AnimState.RightStep1, halfStepTime );
						curve	=	AnimationCurve.QuadraticStep;
					} 
					/*else if (!locomotion)
					{
						ChangeState( AnimState.IdleChangePose, stateTimer );
						idlePose = 0;
					} */
				break;

				case AnimState.RightStep1:
					GenerateTargetPose( takeWalk, 2, takeRun, 1, walkRunFactor );
					if (timeout)
					{
						ChangeState( AnimState.LeftStep0, halfStepTime );
						curve	=	AnimationCurve.QuadraticStepInv;
					}
				break;

				case AnimState.LeftStep0:
					GenerateTargetPose( takeWalk, 4, takeRun, 2, walkRunFactor );
					if (timeout)
					{
						ChangeState( AnimState.LeftStep1, halfStepTime );
						curve	=	AnimationCurve.QuadraticStep;
					}
				break;

				case AnimState.LeftStep1:
					GenerateTargetPose( takeWalk, 6, takeRun, 3, walkRunFactor );
					if (timeout)
					{
						ChangeState( AnimState.RightStep0, halfStepTime );
						curve	=	AnimationCurve.QuadraticStepInv;
					}
					else if (!locomotion)
					{
						ChangeState( AnimState.IdleChangePose, halfStepTime - stateTimer );
						idlePose = 0;
					}
				break;
			}
		}


		void ChangeState( AnimState newState, float minTime, float maxTime = 0 )
		{
			var oldState = state;

			CopyCurrentToOrigin();
			var period		=	MathUtil.Random.NextFloat( minTime, maxTime==0 ? minTime : maxTime );
			state			=	newState;
			statePeriod		=	period;
			stateTimer		=	0;

			//Log.Message("Anim FSM : {0} --> {1} : {2}", oldState, newState, period );
		}


		void GenerateTargetPose( AnimationTake take0, int frame0 )
		{
			GenerateTargetPose( take0, frame0, take0, frame0, 0 );
		}


		void GenerateTargetPose( AnimationTake take0, int frame0, AnimationTake take1, int frame1, float factor )
		{
			var key0 = AnimationKey.Identity;
			var key1 = AnimationKey.Identity;

			for ( int nodeIndex=0; nodeIndex<nodeCount; nodeIndex++ )
			{
				take0.GetKeyByIndex( frame0, nodeIndex, ref key0 ); 
				take1.GetKeyByIndex( frame1, nodeIndex, ref key1 ); 

				targetPose[ nodeIndex ] = AnimationKey.Lerp( key0, key1, factor );
			}
		}


		void BlendPoses( float factor )
		{
			float curveFactor = AnimationUtils.Curve( curve, factor );

			for ( int nodeIndex=0; nodeIndex<nodeCount; nodeIndex++ )
			{
				currentPose[ nodeIndex ] = AnimationKey.Lerp( originPose[ nodeIndex ], targetPose[ nodeIndex ], curveFactor );
			}
		}


		void CopyCurrentToOrigin()
		{
			Array.Copy( currentPose, originPose, nodeCount );
		}


		void CopyTargetToOrigin()
		{
			Array.Copy( targetPose, originPose, nodeCount );
		}


		AnimationKey[] CreatePose()
		{
			var array = new AnimationKey[nodeCount];

			for (int i=0; i<nodeCount; i++)
			{
				array[i]	=	AnimationKey.Identity;
			}

			return array;
		}





		public void Advance( float groundVelocity, GameTime gameTime )
		{
		}




		float idlePoseCooldownTimer = 0;
		float idlePoseTransitionTime;
		int currentIdlePose = -1;
		int nextIdlePose = 0;
		bool idleWaiting = false;

		/*void ApplyIdleAnimation( GameTime gameTime )
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
		}	*/



		void ApplyLocomotionAnimation( float travelledDistance )
		{
			/*float frame		=	travalledDistance / 1.7f * 0.32f * 2;
			float factor	=	frame % 1.0f;
			int frame0		=	(int)Math.Floor( frame );
			int frame1		=	frame0 + 1;			*/

		}

	}
}
