using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;



namespace CoreIK {
	public struct IkHumanTarget {
		
		public Matrix	Basis;
		public Matrix	ChestBasis;
		public Matrix	PelvisBasis;
		public Vector3	HeadLookTarget;
		public Vector3	HeadUp;
		
		public Matrix	LFootPrint;
		public Matrix	RFootPrint;
		public Vector3	LKnee;
		public Vector3	RKnee;

		public Matrix	LHand	;
		public Matrix	RHand	;
		public Vector3	LElbow	;
		public Vector3	RElbow	;

		public struct Fingers {
			public Vector3	Thumb	;
			public Vector3	Index	;	
			public Vector3	Middle	;
			public Vector3	Ring	;
			public Vector3	Pinky	;
		}

		public Fingers	LFingers;
		public Fingers	RFingers;


		public void Draw ( DebugRender dr, float height=2.0f ) 
		{
			//	draw base basis :
			dr.DrawRing( Basis, 1.2f*height/4, Color.Yellow );
			dr.DrawRing( Basis, 1.0f*height/4, Color.Yellow );
			dr.DrawVector( Basis.TranslationVector, Basis.Forward, Color.Yellow, height/2 );

			//	draw chest and pelvis basis :
			dr.DrawRing( PelvisBasis, height/4/2, Color.Yellow );
			dr.DrawRing( ChestBasis,  height/3/2, Color.Yellow );

			//	draw head basis :
			dr.DrawPoint( HeadLookTarget, 0.5f, Color.White );

			//	draw foot prints :
			dr.DrawRing ( LFootPrint, height/10/2, Color.Green,   2 );
			dr.DrawRing ( RFootPrint, height/10/2, Color.Magenta, 2 );

			//	draw hand points :
			dr.DrawBasis( LHand, 0.1f );
			dr.DrawBasis( RHand, 0.1f );

			dr.DrawPoint( LFingers.Thumb	, 0.02f, Color.Yellow );
			dr.DrawPoint( LFingers.Index	, 0.02f, Color.Yellow );
			dr.DrawPoint( LFingers.Middle	, 0.02f, Color.Yellow );
			dr.DrawPoint( LFingers.Ring		, 0.02f, Color.Yellow );
			dr.DrawPoint( LFingers.Pinky	, 0.02f, Color.Yellow );
														   
			dr.DrawPoint( RFingers.Thumb	, 0.02f, Color.Yellow );
			dr.DrawPoint( RFingers.Index	, 0.02f, Color.Yellow );
			dr.DrawPoint( RFingers.Middle	, 0.02f, Color.Yellow );
			dr.DrawPoint( RFingers.Ring		, 0.02f, Color.Yellow );
			dr.DrawPoint( RFingers.Pinky	, 0.02f, Color.Yellow );
		}
	}
}
