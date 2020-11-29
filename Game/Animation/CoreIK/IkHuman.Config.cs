using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core.Mathematics;

namespace CoreIK {
	public partial class IkHuman {

		public struct CurvePoint {
			public Vector3	point;
			public Vector3	tangent;
		}

		/*public struct Curve {
			List<CurvePoint>	points;

			public void	Clear ()
			{
				points.Clear();
			}

			public void	Add ( Vector3 point, Vector3 tangent )
			{
				CurvePoint	cp;
				cp.point	=	point;
				cp.tangent	=	tangent;
			}

			public Vector3 Evaluate ( float t )
			{
				t = t * (points.Count - 1);
				int idx = (int)Math.Floor(t);
				float frac = t % 1.0f;
				return Vector3.Hermite( points[idx].point, points[idx].tangent, points[idx+1].point, points[idx+1].tangent, frac );
			}
		} */

		/*public class WalkingConfig {
			public	float	TakeoffTime;	
			public	float	SwingTime;	
			public	float	LandingTime;	
			public	float	StanceTime;	

			public	float	TakeoffAngle;	
			public	float	SwingAngle;	
			public	float	LandingAngle;	
			public	float	StanceAngle;	

			public	Curve	PelvisPosition;
			public	Curve	PelvisAngles;

			public	Curve	ChestPosition;
			public	Curve	ChestAngles;
		} */


		public float	MaxVelocity					=	8.0f;
		public float	CMassDisplacementThreshold	=	0.01f;

		public float	MinStepTime					=	0.3f;
		public float	MaxStepTime					=	1.0f;
		public float	StepFrequencyFadeRate		=	0.5f;
												
		public float	NormalizationFactor			=	0.24f * 1.8f;

		
		public Vector3	PelvisAngularOscMax			=	new Vector3(-20, -10, -10 );
		public Vector3	PelvisAngularOscMin			=	new Vector3(  0,   0,  0 );

		public Vector3	ChestAngularOscMax			=	new Vector3( 20, 0, -10 );
		public Vector3	ChestAngularOscMin			=	new Vector3( 10, 0,  -5 );

	}
}
