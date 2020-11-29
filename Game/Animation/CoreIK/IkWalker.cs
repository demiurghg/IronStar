using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;

namespace CoreIK
{
	public class IkWalker
	{
		public float	FootLength		{ set; get; }
		public float	StepWidth		{ set; get; }
		public float	StepHeight		{ set; get; }
		public float	StepLength		{ set; get; }

		public float	StanceTime		{ get { return 1 - TakeoffTime - SwingTime - LandingTime; } }
		public float	TakeoffTime		{ set; get; }
		public float	SwingTime		{ set; get; }
		public float	LandingTime		{ set; get; }
		public float	StepRate		{ set; get; }

		public float	StanceAngle		{ set; get; }
		public float	TakeoffAngle	{ set; get; }	
		public float	SwingAngle		{ set; get; }
		public float	LandingAngle	{ set; get; }	

		
		Game	game;

		bool	isWalking;

		float	time;

		/// <summary>
		/// Constructor...
		/// </summary>
		/// <param name="target"></param>
		/// <param name="basis"></param>
		public IkWalker( Game game, IkHumanTarget target, Matrix basis, float stepWidth )
		{
			this.game	=	game;
			StepWidth	=	stepWidth;
			time = 0;
		}


		public void StartWalking ()	{	isWalking = true;	}
		public void StopWalking	 () {	isWalking = false;	}


		public Matrix ComputeFootRollingMatrix ( Matrix basis, float angle )
		{
			var rollMatrix	=	basis * Matrix.RotationAxis( basis.Right, angle );

			if (angle<0) {
				rollMatrix.TranslationVector = basis.TranslationVector + (basis.Forward + rollMatrix.Backward) * FootLength/2;
			} else {																				 
				rollMatrix.TranslationVector = basis.TranslationVector + (basis.Backward + rollMatrix.Forward) * FootLength/2;
			}
			return rollMatrix;
		}



		Matrix FootAnimation ( float t, float rightOffset )
		{
			Matrix	foot = Matrix.Identity;

			var t0 = StanceTime	 ;
			var t1 = TakeoffTime ;
			var t2 = SwingTime	 ;
			var t3 = LandingTime ;

			t %= 1;

			if ( t < t0 ) {
				float frac = t / t0;

				foot.TranslationVector = Vector3.BackwardRH * frac * StepLength + Vector3.Right * rightOffset;
			} 

			if ( t >= t0 && t < t0+t1 ) {
				float frac = (t-t0) / (t0+t1);
			}

			if ( t >= t0+t1 && t < t0+t1+t2 ) {
				float frac = (t-t0-t1) / (t0+t1+t2);
			}

			if ( t > t0+t1+t2 ) {
				float frac = (t-t0-t1-t2) / (t0+t1+t2+t3);
			}


			return foot;
		}


		/// <summary>
		/// Update walker
		/// </summary>
		/// <param name="target">Target for IK solver</param>
		/// <param name="basis">Current character basis</param>
		/// <param name="velocity">Current character linear velocity</param>
		/// <param name="dt">Delta-time</param>
		public void Update ( ref IkHumanTarget target, Matrix basis, Vector3 velocity, float dt )
		{
			//var ds = game.GetService<DebugStrings>();
			//var dr = game.GetService<DebugRender>();

			time += dt;

			target.LFootPrint = FootAnimation( time, -StepWidth / 2 );
			target.RFootPrint = FootAnimation( time,  StepWidth / 2 );
						   

			//ds.Add( string.Format( "isWalking : {0}", isWalking ) );

			

			/*foreach (var m in legL.Traces) dr.DrawRing( m, 0.09f, Color.DarkOrange, 2 );
			foreach (var m in legR.Traces) dr.DrawRing( m, 0.09f, Color.Magenta,  2 );

			legL.Update( dt, basis, velocity, basis.Left * StepWidth/2 );
			legR.Update( dt, basis, velocity, basis.Right * StepWidth/2 );

			var displacement = basis.TranslationVector - GetSupportPoint();

			ds.Add(String.Format("{0}", displacement.Length() ));

			if ( displacement.Length() > 0.001f && CanAddStep() ) {
				AddStep();
				Console.WriteLine("Step");
			} */

			/*legL.Reset( OffsetMatrix( basis, basis.Left  * StepWidth/2 ) );
			legR.Reset( OffsetMatrix( basis, basis.Right * StepWidth/2 ) );*/

			/*target.PelvisBasis	= OffsetMatrix( basis, basis.Up * 0.7f );

			target.LFootPrint	= legL.FootBasis;
			target.RFootPrint	= legR.FootBasis;*/
		}



		/// <summary>
		/// Makes matrix offsetted for give offset value
		/// </summary>
		/// <param name="matrix"></param>
		/// <param name="offset"></param>
		/// <returns></returns>
		public static Matrix OffsetMatrix ( Matrix matrix, Vector3 offset )
		{
			Matrix newMatrix = matrix;
			newMatrix.TranslationVector = newMatrix.TranslationVector + offset;
			return newMatrix;
		}

		
	}
}
