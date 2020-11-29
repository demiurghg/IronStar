using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core.Mathematics;


namespace CoreIK {
	public class IkHumanMotion {

		IkHumanSolver	solver;
		//IkHumanTarget	target;

		//	global motion parameters :
		public Vector3			LocalPelvisPos	=	Vector3.Up * 1.0f;
		public Vector3			HeadLookPoint	=	Vector3.Zero;
		public Vector3			HeadUpVector	=	Vector3.Up;

		//public IkHumanTarget	HumanTarget	{ get { return target; } }


		public IkHumanMotion ( Matrix basis, IkHumanSolver solver )
		{
			this.solver	=	solver;
		}


		public void Move ( Matrix newBasis, Vector3 velocity )
		{
			
		}



		void UpdateWalking ( float dt )
		{
		}



		public void Update ( float dt )
		{
			
		}




		public IkHumanTarget DefaultStance( float height, float time, Matrix basis )
		{
			IkHumanTarget	target;

			//Matrix	basis	=	Matrix.CreateFromYawPitchRoll( 0, 0, 0 );
			Vector3	up				=	basis.Up;
			Vector3 fwd				=	basis.Forward;
			Vector3 rt				=	basis.Right;

			target.Basis			=	basis;
			target.ChestBasis		=	IkSkeleton.AimBasis( fwd, up, up * height * 0.65f + basis.TranslationVector );
            target.PelvisBasis      =	IkSkeleton.AimBasis( fwd, up, up * height * 0.50f + basis.TranslationVector );
			target.HeadLookTarget	=	up * 0.5f + fwd * 4.0f - rt * 4;
			target.HeadUp			=	Vector3.Up;

			target.LElbow			=	-fwd;
			target.RElbow			=	-fwd;
			target.LKnee			=	 fwd;
			target.RKnee			=	 fwd;

            target.RFootPrint		=	IkSkeleton.AimBasis(fwd, up,  rt * 0.2f + basis.TranslationVector);
            target.LFootPrint		=	IkSkeleton.AimBasis(fwd, up, -rt * 0.2f + basis.TranslationVector);
            target.RHand			=	IkSkeleton.AimBasis(fwd, up,  rt * 0.4f + up * 1.2f + fwd * 0.2f + basis.TranslationVector);
            target.LHand			=	IkSkeleton.AimBasis(fwd, up, -rt * 0.4f + up * 1.2f + fwd * 0.2f + basis.TranslationVector);

			float	pw				=	0.08f/4;

			float a = 0.03f + 0.02f * (float)Math.Sin( time );
			float b = 0.03f + 0.02f * (float)Math.Sin( time+1 );
			float c = 0.03f + 0.02f * (float)Math.Sin( time+2 );
			float d = 0.03f + 0.02f * (float)Math.Sin( time+3 );
			float e = 0.03f + 0.02f * (float)Math.Sin( time+4 );

			target.LFingers.Thumb	=	target.LHand.TranslationVector + 0.10f * target.LHand.Forward - target.LHand.Left  * pw * 2.5f + Vector3.Down * a;
			target.LFingers.Index	=	target.LHand.TranslationVector + 0.15f * target.LHand.Forward - target.LHand.Left  * pw * 1.5f + Vector3.Down * b;
			target.LFingers.Middle	=	target.LHand.TranslationVector + 0.15f * target.LHand.Forward - target.LHand.Left  * pw * 0.5f + Vector3.Down * c;
			target.LFingers.Ring	=	target.LHand.TranslationVector + 0.15f * target.LHand.Forward + target.LHand.Left  * pw * 0.5f + Vector3.Down * d;
			target.LFingers.Pinky	=	target.LHand.TranslationVector + 0.15f * target.LHand.Forward + target.LHand.Left  * pw * 1.5f + Vector3.Down * e;
																	   															 
			target.RFingers.Thumb	=	target.RHand.TranslationVector + 0.10f * target.RHand.Forward - target.RHand.Right * pw * 2.5f + Vector3.Down * a;
			target.RFingers.Index	=	target.RHand.TranslationVector + 0.15f * target.RHand.Forward - target.RHand.Right * pw * 1.5f + Vector3.Down * b;
			target.RFingers.Middle	=	target.RHand.TranslationVector + 0.15f * target.RHand.Forward - target.RHand.Right * pw * 0.5f + Vector3.Down * c;
			target.RFingers.Ring	=	target.RHand.TranslationVector + 0.15f * target.RHand.Forward + target.RHand.Right * pw * 0.5f + Vector3.Down * d;
			target.RFingers.Pinky	=	target.RHand.TranslationVector + 0.15f * target.RHand.Forward + target.RHand.Right * pw * 1.5f + Vector3.Down * e;

			return target;
		}
	}
}
