using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;


namespace CoreIK {

	
	public partial class IkHuman {

		IkSkeleton	skeleton;

		public Matrix CurrentBasis { get { return targetBasis; } }

		IkLegSQ	legR;
		IkLegSQ	legL;
		IkBone	pelvis;
		IkBone	chest;
		IkBone	head;
		IkBoneChain2	armL;
		IkBoneChain2	armR;

		int		idxLHip;
		int		idxRHip;
		int		idxPelvis;

		Matrix	targetBasis;
		float	targetFriction;
		Vector3	targetVelocity;
		Vector3	targetAcceleration;
		float	feelVelocity;

		float	averageLegLength;
		float	pelvisWidth;

		Random	rand	= new Random();

		Matrix	worldBasis;
		Game	game;

		StreamWriter walkLog;

		bool	leftStep = true;
		float	stepTime = 99999.0f;
		Vector3 pelvisPos;


		public IkHuman	( Game game, IkSkeleton skel, Matrix worldTransform )
		{
			walkLog		=	File.CreateText("walk.log");

			this.game	=	game;
			worldBasis	=	worldTransform;

			skeleton	=	skel;
			legR		=	new IkLegSQ( skeleton, "RHip", "RShin", "RAncle", "RToe" );
			legL		=	new IkLegSQ( skeleton, "LHip", "LShin", "LAncle", "LToe" );
			pelvis		=	skeleton.ExtractIkBone("Pelvis", Vector3.BackwardRH, Vector3.Up );
			chest		=	skeleton.ExtractIkBone("Chest",  Vector3.BackwardRH, Vector3.Up );
			head		=	skeleton.ExtractIkBone("Head",   Vector3.BackwardRH, Vector3.Up );

			armL		=	new IkBoneChain2( skeleton, "LArm", "LForearm", "LHand", -Vector3.BackwardRH );
			armR		=	new IkBoneChain2( skeleton, "RArm", "RForearm", "RHand", -Vector3.BackwardRH );

			idxLHip		=	skeleton.SkinningData.Bones.IndexOf( "LHip" );
			idxRHip		=	skeleton.SkinningData.Bones.IndexOf( "RHip" );
			idxPelvis	=	skeleton.SkinningData.Bones.IndexOf( "Pelvis" );

			averageLegLength	=	1.3f;//( legR.Length() + legL.Length() ) / 2.0f;
			pelvisWidth			=	( skeleton.SkinningData.Bones["RHip"].BindPose.TranslationVector
									- skeleton.SkinningData.Bones["LHip"].BindPose.TranslationVector ).Length();

			//
			var	fwd	=	worldTransform.Forward;
			var rt	=	worldTransform.Right;
			var up	=	worldTransform.Up;
			var pos	=	worldTransform.TranslationVector;

			pelvis.Aim( pos + up * averageLegLength, fwd, up, skeleton );

			SetStance( worldBasis, true );
		}



		void SetStance ( Matrix worldMatrix, bool forced )
		{	
			var	fwd	=	worldMatrix.Forward;
			var rt	=	worldMatrix.Right;
			var up	=	worldMatrix.Up;
			var pos	=	worldMatrix.TranslationVector;
			var hpw	=	pelvisWidth / 2;

			if (forced) {
				legR.SetInitialStep( Matrix.CreateWorld( pos + rt * hpw, fwd, up ) );
				legL.SetInitialStep( Matrix.CreateWorld( pos - rt * hpw, fwd, up ) );
			}
		}



		public void Move ( Matrix basis, Vector3 velocity, Vector3 acceleration, float friction )
		{
			targetBasis			=	basis;
			targetVelocity		=	velocity;
			targetAcceleration	=	acceleration;
			targetFriction		=	friction;
		}

		
		
		IkLegSQ.Step GenStep ( Matrix basis, Vector3 velocity, Vector3 forward, Vector3 right )
		{
			var absVelocity			=	feelVelocity;// velocity.Length();

			float fwdFactor			=	Vector3.Dot( velocity / absVelocity, forward );
			float sideFactor		=	Vector3.Dot( velocity / absVelocity, right );

			float relV				=	MathUtil.Clamp( absVelocity / MaxVelocity, 0, 1 );
			
			IkLegSQ.Step	step	=	new IkLegSQ.Step();

			step.basis				=	basis;

			step.takeoffAngle		=	- MathUtil.DegreesToRadians( MathUtil.Lerp( 25,  60, relV ) ) * fwdFactor;
			step.landingAngle		=	  MathUtil.DegreesToRadians( MathUtil.Lerp( 10,  15, relV ) ) * fwdFactor;
			step.swingAngle			=	- MathUtil.DegreesToRadians( MathUtil.Lerp( 25,  80, relV ) );
			step.swingHeight		=	  MathUtil.Lerp( 0.05f, 0.45f, relV );
			step.swingRightOffset	=	  0;

			step.takeoffTime		=	MathUtil.Lerp( 0.30f, 0.10f, relV );
			step.swingTime			=	MathUtil.Lerp( 0.35f, 0.85f, relV );
			step.landingTime		=	MathUtil.Lerp( 0.05f, 0.05f, relV );
			step.stanceTime			=	MathUtil.Lerp( 0.30f, 0.00f, relV );

			return step;
		}



		bool AddStep ( Matrix basis, Vector3 velocity, float time )
		{
			if ( velocity.Length() < float.Epsilon ) {
				return false;
			}

			float s = leftStep ? -1 : 1;
			var hpw	=	1.2f * pelvisWidth / 2;
			var	fwd	=	basis.Forward;
			var rt	=	basis.Right;
			var up	=	basis.Up;
			var pos	=	basis.TranslationVector;

			IkLegSQ.Step step = GenStep( Matrix.CreateWorld( pos + s*rt * hpw, fwd + s*0.2f * rt, up ), velocity, fwd, rt );

			bool r;
			if ( leftStep ) {
				pelvisRollOscilator.v = 1;
				r = legL.EnqueueStep( step );
			} else {
				pelvisRollOscilator.v = -1;
				r = legR.EnqueueStep( step );
			}
			if (r) {
				leftStep	= !leftStep;
				points.Add( step.basis );
			}
			return true;
		}



		#region Kinematic integration 
		Vector3 IntegratePosition ( float t, float dt, Vector3 p0, Vector3 v0, Vector3 a0, float f, out Vector3 vel )
		{
			Vector3 p = p0;
			Vector3 v = v0;
			Vector3 a = a0;
			for ( float x=0; x<=t; x+=dt ) {
				a = a0 - v * f;
				v = v + a * dt;
				p = p + v * dt;
			}
			vel = v;
			return p;
		}



		Vector3 PredictPosition ( float t, Vector3 p, Vector3 v, Vector3 a, float friction )
		{
			Vector3 dummy;
			return IntegratePosition( t, 0.01f, p, v, a, friction, out dummy );
		}



		float PredictStepTime ( Vector3 v0, Vector3 a0, float f )
		{
			Vector3 v = v0;
			Vector3 a = a0;
			float dt  = 0.02f;
			float t = 0;

			for ( t=dt; t<MaxStepTime; t+=dt ) {
				var u = NormalizationFactor / (t * t);
				var vv = v.Length();
				if (u < vv) {
					break;
				}
				a = a0 - v*f;
				v = v + a * dt;
			}

			return Math.Max(t, MinStepTime);
		}



		Vector3 PredictVelocity ( float t, Vector3 v, Vector3 a, float friction )
		{
			Vector3 vel;
			IntegratePosition( t, 0.001f, Vector3.Zero, v, a, friction, out vel );
			return vel;
		}
		#endregion


		Vector3 GetSupportPoint ()
		{
			var tr	=	legR.NextStepBasis;
			var tl	=	legL.NextStepBasis;
			return 0.5f * (tr.TranslationVector + tl.TranslationVector);
		}


		Vector3 GetAverageFootPosition () 
		{
			return 0.5f * (legL.Transition() + legR.Transition());
		}



		Vector3 GetActualSupportPoint ()
		{
			var tr	=	legR.NextStepBasis;
			var tl	=	legL.NextStepBasis;
			var wr	=	legR.IsSwingDone();
			var wl	=	legL.IsSwingDone();

			if ( wr!=wl) {
				if (wr) return tr.TranslationVector;
				if (wl) return tl.TranslationVector;
			}
			return GetSupportPoint();
		}



		List<Matrix> points = new List<Matrix>();
		float time = 0;



		bool CanAddStep () {
			if (leftStep) {
				return legL.IsSwingDone() && legR.IsHalfStepDone();
			} else {
				return legR.IsSwingDone() && legL.IsHalfStepDone();
			}
		}



		void UpdateMotion ( float dt )
		{
			//DebugStrings	ds = game.GetService<DebugStrings>();

			if (feelVelocity<targetVelocity.Length()) {
				feelVelocity = targetVelocity.Length();
			} else {
				feelVelocity = MathUtil.Lerp( feelVelocity, targetVelocity.Length(), 1-(float)Math.Pow(StepFrequencyFadeRate, dt) );
			}

			/**/int n = 0;
			/**/int num = points.Count;
			/**/foreach (var m in points) {
			/**/	n++;
			/**/	Color color = n%2==0 ? new Color(1f, 0f, 1f, 1f) : new Color(0f, 0.7f, 0f, 1f);
			/**/
			/**/	skeleton.DebugRender.DrawRing( m.TranslationVector + m.Forward*0.1f, 0.10f, color );
			/**/	skeleton.DebugRender.DrawRing( m.TranslationVector - m.Forward*0.1f, 0.10f, color );
			/**/}
			/**/if (points.Count>32) {
			/**/	points.RemoveAt(0);
			/**/	points.RemoveAt(0);
			/**/}
			
			var	velocity		=	targetVelocity;
			var acceleration	=	targetAcceleration;
			worldBasis			=	targetBasis;

			/**/skeleton.DebugRender.DrawVector( worldBasis.TranslationVector, velocity,		Color.Blue );
			/**/skeleton.DebugRender.DrawVector( worldBasis.TranslationVector, acceleration,	Color.Red );
	
			//	some aliases :
			var	fwd	=	targetBasis.Forward;
			var rt	=	targetBasis.Right;
			var up	=	targetBasis.Up;
			var pos	=	targetBasis.TranslationVector;
			var hpw	=	pelvisWidth / 2;

			var sp	=	GetSupportPoint();
			var wp	=	worldBasis.TranslationVector;
			var displacement = wp - sp;

			//	predict step time and position :
			var pPos		= Vector3.Zero;
			var pStepTime	= PredictStepTime( velocity, acceleration, targetFriction );

			//	step frequency fading-out :			
			if (stepTime > pStepTime) {
				stepTime = pStepTime;
			} else {
				stepTime = MathUtil.Lerp( stepTime, pStepTime, 1-(float)Math.Pow(StepFrequencyFadeRate, dt) );
			}

			pPos  = PredictPosition( stepTime, worldBasis.TranslationVector, velocity, acceleration, targetFriction );

			legL.StepFrequency = 0.5f / stepTime;
			legR.StepFrequency = 0.5f / stepTime;


			var step	= Vector3.Zero;
			
			//	cmass is shifted and we can add step - add step :
			if ( displacement.Length() > CMassDisplacementThreshold ) {
				if (CanAddStep()) {

					var basis = worldBasis;
					basis.TranslationVector = Vector3.Lerp( basis.TranslationVector, pPos, 0.5f );
					step = basis.TranslationVector;

					AddStep( basis, velocity, 2*stepTime );
				}
			}

			//pelvisPos = worldBasis.TranslationVector;
			pelvisPos	=	GetAverageFootPosition().Flattern(Vector3.Up);
			PelvisPosPublic = pelvisPos;

			//	DEBUG STUFF :
			/**/var stepVel = PredictVelocity( 0.1f, velocity, acceleration, targetFriction );
			/**/
			/**/ds.Add( string.Format( "step time     : {0,8:0.00}", stepTime ) );
			/**/ds.Add( string.Format( "velocity      : {0,8:0.00}", velocity.Length() ) );
			/**/ds.Add( string.Format( "step velocity : {0,8:0.00}", stepVel.Length() ) );
			/**/
			/**/
			/**/string line = String.Format("{0,16} {1,16} {2,16} {3,16} {4,16} {5,16} {6,16} {7,16} {8,16}", 
			/**/							time, 
			/**/							stepTime,
			/**/							worldBasis.TranslationVector.Z,
			/**/							pPos.Z,
			/**/							pelvisPos.Z,
			/**/							velocity.Z,
			/**/							stepVel.Z,
			/**/							acceleration.Z,
			/**/							step.Z
			/**/							);
			/**/
			/**/walkLog.WriteLine( line );
			/**/walkLog.Flush();
			/**/time += dt;

		}	
			
		public Vector3 PelvisPosPublic;
			
		public void Draw ()
		{
			var up = Vector3.Up * 0.25f;
			skeleton.DebugRender.DrawRing		( worldBasis.TranslationVector + up,	0.4f, new Color(0,1,0,1.0f) );
			skeleton.DebugRender.DrawVector		( worldBasis.TranslationVector + up,	worldBasis.Forward, new Color(0,1,0,1.0f) );
			skeleton.DebugRender.DrawRing		( GetSupportPoint() + up,	0.1f, new Color(1,0,0,1.0f) );

			if (targetBasis!=null) {
				skeleton.DebugRender.DrawRing	( targetBasis.TranslationVector + up*3,	0.4f, new Color(1,0,0,1.0f) );
				skeleton.DebugRender.DrawVector	( targetBasis.TranslationVector + up*3,	targetBasis.Forward, new Color(1,0,0,1.0f) );
			}
		}



		class Oscillator {
			public float x;		//	offset
			public float k;		//	spring elasticty
			public float m;		//	mass
			public float d;		//	damping
			public float v;		//	current velocity

			public void Update ( float dt ) {
				float f = -x * k - v * d;
				float a =  f / m;
				v = v + a * dt;
				x = x + v * dt;
			}
		}


		Oscillator pelvisRollOscilator = new Oscillator() { k=8.0f, m=1, d=2.1f } ;

		float t = 0;
		//float oscillation = 0;

		float Oscillation1 ( float phase )
		{
			return (legR.Oscilator( 1, phase ) - legL.Oscilator( 1, phase )) / 2;
		}

		float Oscillation2 ( float phase )
		{
			return (legR.Oscilator( 2, phase ) + legL.Oscilator( 2, phase )) / 2;
		}

		float Rad ( float x ) {
			return MathUtil.DegreesToRadians(x);
		}


		float currentVertOffset = 0;

		public void UpdatePelvis ( float dt )
		{
			t += dt;
			/*var dp = game.GetService<DebugPlot>();
			dp.PlotRectangle	=	new Rectangle( 440, 10, 830, 200 );
			dp.AxisX	=	"Time (s)";
			dp.AxisY	=	"Value";
			dp.MinPoint	=	new Vector2(  t-4, -3);
			dp.MaxPoint	=	new Vector2(  t+1,  3);
			dp.XTickInterval	=	1;
			dp.YTickInterval	=	1;

			dp.AddValue( "SL",  t, legL.IsInAir()? -1.8f : -2.0f, Color.Magenta, 2000 );
			dp.AddValue( "SR",  t, legR.IsInAir()? -2.2f : -2.4f, Color.Green,	 2000 );

			DebugStrings	ds = game.GetService<DebugStrings>(); */

			float	stepLength	=	stepTime * targetVelocity.Length();


			var tr		=	legR.Transition();
			var tl		=	legL.Transition();
			var htr		=	legR.CurrentFootPos;
			var htl		=	legL.CurrentFootPos;
			var	fwd		=	targetBasis.Forward;
			var rt		=	targetBasis.Right;
			var up		=	targetBasis.Up;
			var pos		=	pelvisPos;
			var lr		=	(tr - tl).Normalized();
			var fwd2	=	Vector3.Cross( up, lr );

			float	relVel			=	feelVelocity / MaxVelocity;
			dp.AddValue( "relVel",  t, relVel, Color.Blue,	 2000 );

			float	oscD0			=	Oscillation1(0);
			float	oscDh			=	Oscillation1(0.5f);
			float	oscDq			=	Oscillation1(0.25f);

			float targetVertOffset	=	0;
			if (targetVelocity.Length()>0.5f) targetVertOffset = 0*0.07f;
			if (targetVelocity.Length()>1.5f) targetVertOffset = 0*0.14f;
			if (targetVelocity.Length()>2.5f) targetVertOffset = 0*0.10f;
			if (targetVelocity.Length()>3.5f) targetVertOffset = 0*0.10f;
			if (targetVelocity.Length()>4.5f) targetVertOffset = 0*0.10f;
			if (targetVelocity.Length()>5.5f) targetVertOffset = 0*0.06f;
			if (targetVelocity.Length()>6.5f) targetVertOffset = 0*0.03f;
			if (targetVelocity.Length()>7.5f) targetVertOffset = 0*0.00f;


			currentVertOffset		=	MathUtil.Lerp( currentVertOffset, targetVertOffset, 1-(float)Math.Pow(0.5f, dt) );

			float	stanceHeight	=	0.95f;
			float	vertOffsetRun	=	0.1f * relVel * (float)Math.Pow(0.5f+0.5f*Oscillation2(0.25f), 2.0f);
			float	vertOffset		=	currentVertOffset + vertOffsetRun;

			float	PelvisHeight	=	stanceHeight - vertOffset;
			dp.AddValue( "VOfs1",   t, -vertOffset*10, Color.LightCyan,	 2000 );
			dp.AddValue( "VOfs2",   t, -vertOffset*10, Color.LightBlue,	 2000 );
			dp.AddValue( "Fall",   t, stepLength, Color.Red,		 2000 );


			//	Pelvis :
			float	pelvisYaw		=	MathUtil.Lerp( Rad( PelvisAngularOscMin.X ), Rad( PelvisAngularOscMax.X ), relVel ) * Oscillation1(0);
			float	pelvisPitch		=	MathUtil.Lerp( Rad( PelvisAngularOscMin.Y ), Rad( PelvisAngularOscMax.Y ), relVel ) * 1;
			float	pelvisRoll		=	MathUtil.Lerp( Rad( PelvisAngularOscMin.Z ), Rad( PelvisAngularOscMax.Z ), relVel ) * oscD0;

			Vector3	pelvisShift		=	MathUtil.Lerp( 0.0f, 0.1f, relVel ) * rt * Oscillation1(0);
								
			Matrix	postXForm0		=	Matrix.RotationYawPitchRoll( pelvisYaw, pelvisPitch, pelvisRoll ); 
			Matrix	pelvisMatrix	=	pelvis.Aim( pos + up*PelvisHeight + pelvisShift, fwd, up, postXForm0, skeleton );

			//	Chest :
			float	chestYaw		=	MathUtil.Lerp( Rad( ChestAngularOscMin.X ), Rad( ChestAngularOscMax.X ), relVel*relVel ) * Oscillation1(-0.125f);
			float	chestPitch		=	MathUtil.Lerp( Rad( ChestAngularOscMin.Y ), Rad( ChestAngularOscMax.Y ), relVel ) * oscD0;
			float	chestRoll		=	MathUtil.Lerp( Rad( ChestAngularOscMin.Z ), Rad( ChestAngularOscMax.Z ), relVel ) * oscD0;
								
			Matrix	postXForm1		=	Matrix.RotationYawPitchRoll( 0.1f*chestYaw-0.5f, chestPitch, chestRoll ); 
			Matrix chestMatrix		=	skeleton.SkinningData.Bones["Chest"].LocalBindPose * pelvisMatrix;
			Vector3	chestShift		=	- MathUtil.Lerp( 0.0f, 0.1f, relVel ) * rt * Oscillation1(0);
								

			chestMatrix				=	chest.Aim( chestMatrix.TranslationVector + chestShift, worldBasis.Forward + fwd, 
											   Vector3.Up, 
											   postXForm1,
											   skeleton );

			skeleton.DebugRender.DrawBasis( chestMatrix, 1 );

			skeleton.TransformBone("Chest", chestMatrix);
			skeleton.TransformChildren( "Chest", chestMatrix );

			Matrix headMatrix = skeleton.SkinningData.Bones["Head"].LocalBindPose * chestMatrix;
			head.Aim( headMatrix.TranslationVector, worldBasis.Forward, Vector3.Up, skeleton );

			//	HANDS :
			var shoulderL = chest.GetGlobalPoint( skeleton.SkinningData.Bones["LArm"].LocalBindPosition );
			var shoulderR = chest.GetGlobalPoint( skeleton.SkinningData.Bones["RArm"].LocalBindPosition );

			var las = armL.Solve( shoulderL, up + pos - 0.3f*rt + 0.2f*fwd * (0.1f - Oscillation1(0) * relVel), -fwd );
			var ras = armR.Solve( shoulderR, up + pos + 0.3f*rt + 0.2f*fwd * (0.1f + Oscillation1(0) * relVel), -fwd );

			las = armL.Solve( shoulderL, armBindL, -fwd );
			ras = armR.Solve( shoulderR, armBindR, -fwd );

			skeleton.TransformChildren( "LForearm", las.bone1 );
			skeleton.TransformChildren( "RForearm", ras.bone1 );


		}

		Vector3 armBindR;
		Vector3 armBindL;

		public void BindArms ( Vector3 posR, Vector3 posL )
		{
			armBindR	=	posR;
			armBindL	=	posL;
		}



		public void Update ( float dt )
		{
			UpdateMotion( dt );
			UpdatePelvis( dt );

			var hipPosR = pelvis.GetGlobalPoint( skeleton.SkinningData.Bones[ idxRHip ].LocalBindPosition );
			var hipPosL = pelvis.GetGlobalPoint( skeleton.SkinningData.Bones[ idxLHip ].LocalBindPosition );

			legR.Update( dt, hipPosR );
			legL.Update( dt, hipPosL );
		}



		float LinearDistance ( Matrix t0, Matrix t1 )
		{
			var p0	=	t0.TranslationVector;
			var p1	=	t1.TranslationVector;
			return (p0 - p1).Length();
		}



		float AngularDistance ( Matrix t0, Matrix t1 )
		{
			var q0	=	Quaternion.RotationMatrix( t0 );
			var q1	=	Quaternion.RotationMatrix( t1 );
			q0.Normalize();
			q1.Normalize();
			var dot	=	Quaternion.Dot( q0, q1 );
			return 2 * (float)Math.Acos( dot );
		}


	}
}
