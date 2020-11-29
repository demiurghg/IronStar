using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusion.Core.Mathematics;
using IronStar.Animation;

namespace CoreIK {
	public class IkHumanFootDriver {

		//	foot length required for correct foot rolling :
		public	float	FootLength		=	0.4f;

		//	foot phase temporal and angular parameters :
		public	float	TakeoffAngle	=	15;
		public	float	SwingAngle		=	30;
		public	float	LandingAngle	=	10;
		public	float	StanceAngle		=	 0;
										
		public	float	TakeoffTime		=	0.10f;
		public	float	SwingTime		=	0.50f;
		public	float	LandingTime		=	0.10f;
		public	float	StanceTime		=	0.30f;

		public	float	StepHeight		=	0.30f;

		public	float	StepFrequency	=	1.0f;

		//	foot and leg parameters :
		public	Matrix	FootBasis		{ get; protected set; }
		public	Vector3	KneeDirection	{ get; protected set; }

		//	internal data :
		bool	done		=	true;
		float	stepTime	=	0;
		Matrix	prevBasis;
		Matrix	nextBasis;


		
		public void SetFoot ( Matrix footBasis )
		{
			done		=	true;
			stepTime	=	0;
			prevBasis	=	footBasis;
			nextBasis	=	footBasis;
		}


		enum StepPhase { Takeoff, Swing, Landing, Stance	}

		//void GetStepPhase ( out StepPhase stepPhase, out float timeFrac )
		//{
		//}


		public void Update ( float dt )
		{
			stepTime += dt;
			if ( stepTime >= 1 ) {
				done = true;
				return;
			}
			
			Matrix	footProj	=	AnimationUtils.Lerp( prevBasis, nextBasis, stepTime );
			KneeDirection		=	footProj.Forward;

			//	temporary solution :
			FootBasis			=	footProj;
		}


		public bool IsDone ()  		{ return done;	}
		public bool IsHalfDone () 	{ return (stepTime > 0.5f);	}



		public void SetTargetFootBasis ( Matrix targetFootBasis )
		{
			nextBasis	=	targetFootBasis;
		}


		public void NextStep ( Matrix targetFootBasis )
		{
			done		=	false;
			stepTime	%=	1;
			prevBasis	=	nextBasis;
			nextBasis	=	targetFootBasis;
		}
	}
}
