using System;
using System.Collections.Generic;
using Fusion.Core.Mathematics;
using IronStar.Animation;

namespace CoreIK {
	public class IkLeg2 {
		
		public float	TakeoffTime		{ set; get; }
		public float	SwingTime		{ set; get; }
		public float	LandingTime		{ set; get; }
		public float	StepRate		{ set; get; }

		float	stepTime = 0;
		bool	stepDone = true;

		public bool		IsOnGround		{ get { return (stepTime < TakeoffTime) || (stepTime > TakeoffTime + SwingTime); } }
		public bool		IsStepDone		{ get { return stepDone; } }
		public bool		IsHalfStepDone	{ get { return stepTime > 0.5f || stepDone; } }

		public Matrix	FootBasis		{ get; protected set; }
		public Matrix	NextFootBasis;
		public Matrix	PrevFootBasis	{ get; protected set; }

		public List<Matrix>	Traces	=	new List<Matrix>();
	

		public IkLeg2 ()
		{
		}



		public void Reset ( Matrix footBasis )
		{
			NextFootBasis	=	footBasis;
			PrevFootBasis	=	footBasis;
			FootBasis		=	footBasis;
			stepTime		=	0;
			stepDone		=	true;
		}



		public void Update ( float dt, Matrix basis, Vector3 velocity, Vector3 sideOffset )
		{
			if (Traces.Count>10) { Traces.RemoveAt(0); }

			if (!stepDone) {
				stepTime += dt * StepRate;
			}

			if (stepTime > 1) {
				stepTime		= 0;
				stepDone		= true;
				PrevFootBasis	= NextFootBasis;
				Traces.Add( PrevFootBasis );
			}

			if (!IsOnGround) {
				NextFootBasis = basis;
				NextFootBasis.TranslationVector = basis.TranslationVector + velocity / StepRate + sideOffset;
			}

			float t		=	MathUtil.Clamp( (stepTime - TakeoffTime) / SwingTime, 0, 1 );
			FootBasis	=	AnimationUtils.Lerp( PrevFootBasis, NextFootBasis, t );
		}



		public void AddStepForced ()
		{
			stepTime = 0;
			stepDone = false;
		}

	}
}
