using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Fusion.Core.Mathematics;


namespace CoreIK {

	public class IkLegSQ : IkLeg {

		public struct Step {
			public Matrix	basis		;
			public float	takeoffAngle;
			public float	stanceAngle	;
			public float	landingAngle;

			public float	swingAngle;
			public float	swingHeight;
			public float	swingRightOffset;

			public float	takeoffTime	;
			public float	swingTime	;
			public float	landingTime	;
			public float	stanceTime	;

			public static Step Create ( Matrix basis, float takeoffAngle, float landingAngle, float stanceAngle, 
			   float takeoffTime, float swingTime, float landingTime, float stanceTime, float swingAngle, float swingHeight, float swingRightOffset )
			{
				var step = new Step();
				step.basis				=	basis		 ;	
				step.takeoffAngle		=	MathUtil.DegreesToRadians( takeoffAngle );	
				step.stanceAngle		=	MathUtil.DegreesToRadians( stanceAngle  );	
				step.landingAngle		=	MathUtil.DegreesToRadians( landingAngle );	
				step.takeoffTime		=	takeoffTime	 ;	
				step.swingTime			=	swingTime	 ;	
				step.landingTime		=	landingTime	 ;	
				step.stanceTime			=	stanceTime	 ;	

				step.swingAngle			=	MathUtil.DegreesToRadians( swingAngle   );	
				step.swingHeight		=	swingHeight		 ;
				step.swingRightOffset	=	swingRightOffset ;	

				return step;
			} 
		}

		float		stepTime;

		Step		prevStep;
		Step		nextStep;			

		public Matrix	PrevStepBasis { get { return prevStep.basis; } }
		public Matrix	NextStepBasis { get { return nextStep.basis; } }
		public float	SwingTimeFraction { get { return MathUtil.Clamp( stepTime / base.TotalSwingTime, 0, 1 ); } }
		public float	StepTimeFraction  { get { return MathUtil.Clamp( stepTime / base.TotalStepTime , 0, 1 ); } }

		

		public float StepFrequency = 1.0f;

		public IkLegSQ ( IkSkeleton skel, string hipName, string shinName, string footName, string toeName )
		 : base( skel, hipName, shinName, footName, toeName  )
		{
			
		}



		public void Update ( float dtime, Vector3 hipOrigin ) 
		{	
			if ( !IsStepDone() ) {
				stepTime += dtime * StepFrequency;
			} 
			base.InterpolateStep( hipOrigin, stepTime );
		}



		public bool IsStepDone ()		{ return (stepTime >= TotalStepTime);	}
		public bool IsSwingDone ()		{ return (stepTime >= TotalSwingTime);	}
		public bool IsSwinging()		{ return (stepTime <= TotalSwingTime);	}
		public bool IsHalfStepDone ()	{ return (stepTime >= TotalStepTime / 2);	}
		public bool IsInAir()			{ return (stepTime > FootTakeOffTime && stepTime <= (FootSwingTime + FootTakeOffTime) );	}



		public void ForceStep ()
		{
			InitStepInterpolator();
			stepTime = TotalStepTime;
		}



		public float Oscilator (float freq=1, float phase=0) 
		{
			float t = MathUtil.Clamp( stepTime / TotalStepTime, 0, 1 );
			return (float)Math.Sin( t * 2*Math.PI * freq + phase*2*Math.PI );
		}



		public Vector3 Transition () 
		{
			float t = MathUtil.Clamp( stepTime / TotalStepTime, 0, 1 );
			return Vector3.Lerp( prevStep.basis.TranslationVector, nextStep.basis.TranslationVector, t );
		}



		public void SetInitialStep ( Matrix basis )
		{
			var step = new Step();
			step.basis			=	basis ;	
			step.takeoffAngle	=	0;	
			step.stanceAngle	=	0;	
			step.landingAngle	=	0;	
			step.takeoffTime	=	0.1f;	
			step.swingTime		=	0.1f;	
			step.landingTime	=	0.1f;	
			step.stanceTime		=	0.1f;	
			prevStep			=	step;
			nextStep			=	step;
			InitStepInterpolator();
			stepTime			=	TotalStepTime;
		}



		public bool EnqueueStep ( Matrix basis, float takeoffAngle, float landingAngle, float stanceAngle, 
						   float takeoffTime, float swingTime, float landingTime, float stanceTime )
		{
			var step			=	new Step();
			step.basis			=	basis		 ;	
			step.takeoffAngle	=	MathUtil.DegreesToRadians( takeoffAngle );	
			step.stanceAngle	=	MathUtil.DegreesToRadians( stanceAngle  );	
			step.landingAngle	=	MathUtil.DegreesToRadians( landingAngle );	
			step.takeoffTime	=	takeoffTime	 ;	
			step.swingTime		=	swingTime	 ;	
			step.landingTime	=	landingTime	 ;	
			step.stanceTime		=	stanceTime	 ;	

			return EnqueueStep( step );
		}


		public bool EnqueueStep ( Step step )
		{
			if ( IsSwingDone() ) {
				prevStep			=	nextStep;
				nextStep			=	step;
				InitStepInterpolator();
				return true;
			} else {
				Console.WriteLine("ERROR!");
			}
			return false;
		}


		void InitStepInterpolator ()
		{
			//	get properties from previous step :
			base.FootBasis0			=	prevStep.basis;
			base.FootStanceAngle0	=	prevStep.stanceAngle;
											
			//	get properties from next step :
			base.FootBasis1			=	nextStep.basis;
			base.FootTakeOffAngle	=	nextStep.takeoffAngle;
			base.FootTakeOffTime	=	nextStep.takeoffTime;
			base.FootSwingTime		=	nextStep.swingTime;
			base.FootLandingAngle	=	nextStep.landingAngle;
			base.FootLandingTime	=	nextStep.landingTime;
			base.FootStanceAngle1	=	nextStep.stanceAngle;
			base.FootStanceTime		=	nextStep.stanceTime;
			base.FootSwingAngle		=	nextStep.swingAngle;
			base.FootSwingHeight	=	nextStep.swingHeight;
			base.FootSwingSideOffset=	nextStep.swingRightOffset;

			if (stepTime>1) {
				stepTime =	stepTime - 1;
			} else {
				stepTime = 0;
			}
		}
	}
}
