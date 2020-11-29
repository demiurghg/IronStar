using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Fusion.Core.Mathematics;
using IronStar.Animation;

namespace CoreIK {

	public class IkLeg {
			
		public Vector3	HipJoint { get; private set; }
		IkSkeleton		skeleton;
		IkBoneChain2	boneChain;
		IkBone			foot;
		IkBone			toe;
		float			ancleHeight;
		float			toeSize;
		float			clearence;			//	"sole with" - distance between floor and toe joint (from model)
		Vector3			toeFoot;			//	vector from toe-joint to foot-joint
		Vector3			heelFoot;			//	vector from heel to foot-joint
		Vector3			heelToe;

		public Vector3	LastAnclePosition { get; protected set; }

		public IkLeg ( IkSkeleton skel, string hipName, string shinName, string ancleName, string toeName )
		{
			var kneeBendDir =	Vector3.BackwardRH;
		    skeleton		=	skel;

			kneeBendDir.Normalize();
			var flatBendDir	=	kneeBendDir.Flattern( Vector3.Up );

		    boneChain		=	new IkBoneChain2( skel, hipName, shinName, ancleName, kneeBendDir );
		    foot			=	skeleton.ExtractIkBone( ancleName, flatBendDir, Vector3.Up );
			toe				=	skeleton.ExtractIkBone( toeName,  flatBendDir, Vector3.Up );

			ancleHeight		=	Vector3.Dot( skeleton.SkinningData.Bones[ ancleName ].BindPose.TranslationVector, Vector3.Up ) - clearence;
			clearence		=	Vector3.Dot( skeleton.SkinningData.Bones[ toeName ].BindPose.TranslationVector, Vector3.Up );

			Vector3	footPos	=	skeleton.SkinningData.Bones[ ancleName ].BindPose.TranslationVector;
			Vector3	toePos	=	skeleton.SkinningData.Bones[ toeName ].BindPose.TranslationVector;
			toeFoot			=	footPos - toePos;
			Vector3 heelPos	=	skeleton.SkinningData.Bones[ toeName ].BindPose.TranslationVector - toeFoot.Flattern( Vector3.Up ).Expand( ancleHeight );
			heelFoot		=	footPos - heelPos;
			heelToe			=	toePos - heelPos;


			//MaxDistance		=	b

			//	temporary :
			this.toeSize	=	heelToe.Length() / 4.0f;
		}


		public float MaxDistance() { return boneChain.MaxDistance(); }
		public float MinDistance() { return boneChain.MinDistance(); }


		public void Solve ( Vector3 hipOrigin, Vector3 footTarget, Vector3 initBendDir )
		{
			var sln = boneChain.Solve( hipOrigin, footTarget, initBendDir );

			skeleton.globalBones[ foot.Index ] = skeleton.SkinningData.Bones[ foot.Index ].LocalBindPose * sln.bone1;
			skeleton.globalBones[ toe.Index ] = skeleton.SkinningData.Bones[ toe.Index ].LocalBindPose * skeleton.globalBones[ foot.Index ];
		}



		public void Solve ( Vector3 hipOrigin, Matrix footBasis, Vector3 initBendDir )
		{
			Vector3 footFwd = footBasis.Forward;
			Vector3	footRt	= footBasis.Right;
			Vector3 footUp  = footBasis.Up;

			Vector3 footPos	= AnclePosition( footBasis );
			TargetFootPos	= footPos;

			LastAnclePosition = footPos;

			var sln = boneChain.Solve( hipOrigin, footPos, initBendDir );

			footPos = sln.hitPos;
			
			CurrentFootPos = footPos;

			foot.Aim( footPos, footFwd, footUp, skeleton );
			
			Vector3 toePos	= footPos + foot.GetGlobalVector( -toeFoot );
			toe.Aim( toePos, footFwd, footUp, skeleton );
		}



		public Vector3 AnclePosition ( Matrix footBasis )
		{
			Vector3 footFwd = footBasis.Forward;
			Vector3	footRt	= footBasis.Right;
			Vector3 footUp  = footBasis.Up;

			return footBasis.TranslationVector + footFwd * ( toeFoot.Length() - 2 * ancleHeight ) + footUp.Resize( ancleHeight );
		}



		public Vector3 CurrentFootPos;
		public Vector3 TargetFootPos;

		public void Solve2 ( Vector3 hipOrigin, Vector3 toeTarget, Vector3 heelTarget, Vector3 initBendDir, Vector3 up, float toeFactor=0 )
		{
			Vector3 footFwd = toeTarget - heelTarget;
			Vector3	footRt	= Vector3.Cross( footFwd, up );
			Vector3 footUp  = Vector3.Cross( footRt, footFwd );

			Vector3 footPos	= heelTarget + footFwd.Resize( ancleHeight ) + footUp.Resize( ancleHeight );
			TargetFootPos	= footPos;

			LastAnclePosition = footPos;

			var sln = boneChain.Solve( hipOrigin, footPos, initBendDir );
			//var ofs = footPos - sln.hitPos;

			//sln = boneChain.Solve( hipOrigin + ofs, footPos, initBendDir );
			footPos = sln.hitPos;
			
			CurrentFootPos = footPos;

			foot.Aim( footPos, footFwd, footUp, skeleton );
			
			Vector3 toePos	= footPos + foot.GetGlobalVector( -toeFoot );
			toe.Aim( toePos, footFwd, footUp, skeleton );
		}

		


		/*---------------------------------------------------------------------
		 *
		 *	 STEP INTERPOLATION :
		 * 
		---------------------------------------------------------------------*/

		public float	FootTakeOffTime;		//	time to roll foot before leg swing 
		public float	FootLandingTime;		//	time to roll foot after swing
		public float	FootStanceTime;			//	time while foot is on the ground
		public float	FootSwingTime;			//	time while foot is on the air

		public Matrix	FootBasis0;
		public Matrix	FootBasis1;

		public float	FootTakeOffAngle;
		public float	FootLandingAngle;
		public float	FootStanceAngle0;
		public float	FootStanceAngle1;

		public float	FootSwingAngle;
		public float	FootSwingHeight = 0;
		public float	FootSwingSideOffset = 0;


		public float	TotalStepTime {
			get { return FootTakeOffTime + FootLandingTime + FootSwingTime + FootStanceTime; }
		}


		public float	TotalSwingTime {
			get { return FootTakeOffTime + FootLandingTime + FootSwingTime; }
		}


		
		public static Matrix ComputeFootRollingMatrix ( Matrix basis, float angle, float footLen )
		{
			var rollMatrix	=	basis * Matrix.RotationAxis( basis.Right, angle );

			if (angle<0) {
				rollMatrix.TranslationVector = basis.TranslationVector + (basis.Forward + rollMatrix.Backward) * footLen/2;
			} else {
				rollMatrix.TranslationVector = basis.TranslationVector + (basis.Backward + rollMatrix.Forward) * footLen/2;
			}
			return rollMatrix;
		}


		public Matrix ComputeFootSwingMatrix ()
		{
			var	tfs	=	AnimationUtils.Lerp( FootBasis0, FootBasis1, 0.5f );
			var up	=	tfs.Up;
			var rt	=	tfs.Right;

			tfs = tfs * Matrix.RotationAxis( tfs.Right, FootSwingAngle );

			tfs.TranslationVector =  Vector3.Lerp( FootBasis0.TranslationVector, FootBasis1.TranslationVector, 0.5f );
			tfs.TranslationVector += up * FootSwingHeight;
			tfs.TranslationVector += rt * FootSwingSideOffset;
			return tfs;
		}


		public void InterpolateStep ( Vector3 hipOrigin, float time )
		{
			float footLen = heelToe.Length();
			Matrix	tf0	=	ComputeFootRollingMatrix ( FootBasis0, FootStanceAngle0, footLen );
			Matrix	tf1	=	ComputeFootRollingMatrix ( FootBasis0, FootTakeOffAngle, footLen );
			Matrix	tfs	=	ComputeFootSwingMatrix();
			Matrix	tf2	=	ComputeFootRollingMatrix ( FootBasis1, FootLandingAngle, footLen );
			Matrix	tf3	=	ComputeFootRollingMatrix ( FootBasis1, FootStanceAngle1, footLen );

			//skeleton.DebugRender.DrawBasis( tf0, 0.3f );
			//skeleton.DebugRender.DrawBasis( tf1, 0.3f );
			//skeleton.DebugRender.DrawBasis( tf2, 0.3f );
			//skeleton.DebugRender.DrawBasis( tf3, 0.3f );
			//skeleton.DebugRender.DrawBasis( tfs, 0.4f );

			float factor = MathUtil.Clamp( time / TotalSwingTime, 0, 1 );

			Matrix	footBasis  = AnimationUtils.Lerp( FootBasis0, FootBasis1, factor );
			
			float t0	=	FootTakeOffTime;
			float t1	=	FootSwingTime;
			float t2	=	FootLandingTime;
			float t3	=	FootStanceTime;

			float	takeoffFootSpeed = Math.Abs(FootTakeOffAngle) / FootTakeOffTime * footLen/2;
			float	landingFootSpeed = Math.Abs(FootLandingAngle) / FootLandingTime * footLen/2;

			Matrix	footMatrix;

			//	Takeoff :
			if ( time < t0 ) {
			    footMatrix	=	AnimationUtils.Lerp( tf0, tf1, time / t0 );

			} else
			//	Swing :
			if ( time < t0+t1 ) {

				//var hp0 = tf1.TranslationVector;
				//var hp1 = tf2.TranslationVector;
				//var ht0 = tf1.Up * takeoffFootSpeed;
				//var ht1 = tf2.Down * landingFootSpeed;
				//var frc = (time-t0) / t1;

				//footMatrix	=	MathX.LerpMatrix( tf1, tf2, frc );
				//footMatrix.TranslationVector = Vector3.Hermite( hp0, ht0, hp1, ht1, frc );

				var p0	 = tf0.TranslationVector;
				var p1	 = tf1.TranslationVector;
				var ps	 = tfs.TranslationVector;
				var p2	 = tf2.TranslationVector;
				var p3	 = tf3.TranslationVector;
				var frc	 = (time-t0) / t1;
				
				if (frc < 0.5) {
					footMatrix	=	AnimationUtils.Lerp( tf1, tfs, frc*2 );
					footMatrix.TranslationVector = Vector3.CatmullRom( p0, p1, ps, p2, frc*2 );

				} else {
					footMatrix	=	AnimationUtils.Lerp( tfs, tf2, frc*2-1 );
					footMatrix.TranslationVector = Vector3.CatmullRom( p1, ps, p2, p3, frc*2-1 );
				}


			} else 
			//	Landing :
			if ( time < t0+t1+t2 ) {
			    footMatrix	=	AnimationUtils.Lerp( tf2, tf3, (time-t0-t1) / t2 );

			} else {
			    footMatrix	=	tf3;
			}


			var toe		=	footMatrix.TranslationVector + footMatrix.Forward * footLen/2;
			var heel	=	footMatrix.TranslationVector + footMatrix.Backward * footLen/2;

			Solve2( hipOrigin, toe, heel, footBasis.Forward, footBasis.Up );
		}

	}
}
