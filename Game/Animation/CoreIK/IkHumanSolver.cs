using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;



namespace CoreIK {
	public class IkHumanSolver : IkSkeleton {

		//	human metrics :
		public	float	HumanHeight		{ get; protected set; }
		public	float	LeftFootLength	{ get; protected set; }
		public	float	RightFootLength	{ get; protected set; }	
		public	float	ShoulderWidth	{ get; protected set; }
		public	float	PelvisWidth		{ get; protected set; }
		public	float	LPalmWidth		{ get; protected set; }
		public	float	RPalmWidth		{ get; protected set; }

		IkBone			head;
		IkBone			pelvis;
		IkBone			chest;
		IkBoneChain2	armL;
		IkBoneChain2	armR;
		IkLeg			legL;
		IkLeg			legR;
		IkBone			handL;
		IkBone			handR;

		Vector3			headJoint;
		Vector3			chestJoint;
		Vector3			hipJointL;
		Vector3			hipJointR;
		Vector3			armJointL;
		Vector3			armJointR;

		struct Fingers {
			public Vector3		thumb;
			public Vector3		index;
			public Vector3		middle;
			public Vector3		ring;
			public Vector3		pinky;
			public IkBoneChain3	thumbChain;
			public IkBoneChain3	indexChain;
			public IkBoneChain3	middleChain;
			public IkBoneChain3	ringChain;
			public IkBoneChain3	pinkyChain;
		}

		Fingers	fingersL;
		Fingers	fingersR;


		IkParticleSystem	ikPrts;

		enum PrtIdx {
			Chest,	
			LHip,	RHip,
			LArm,	RArm,	
			Max,
		}
		
		public IkHumanSolver ( Game game, Model model ) : base( model, game.GetService<DebugRender>() )
		{
			//	get bones and chains :
			pelvis			=	ExtractIkBone("Pelvis", Vector3.BackwardRH, Vector3.Up );
			chest			=	ExtractIkBone("Chest",  Vector3.BackwardRH, Vector3.Up );
			head			=	ExtractIkBone("Head",   Vector3.BackwardRH, Vector3.Up );

			legL			=	new IkLeg( this, "LHip", "LShin", "LAncle", "LToe" );
			legR			=	new IkLeg( this, "RHip", "RShin", "RAncle", "RToe" );

			armL			=	new IkBoneChain2( this, "LArm", "LForearm", "LHand", -Vector3.BackwardRH );
			armR			=	new IkBoneChain2( this, "RArm", "RForearm", "RHand", -Vector3.BackwardRH );

			var handUpL		=	Vector3.Cross( -Vector3.ForwardRH, GlobalBoneToBoneVector( "LHand", "LMiddle1" ) );
			var handUpR		=	Vector3.Cross(  Vector3.ForwardRH, GlobalBoneToBoneVector( "RHand", "RMiddle1" ) );

			handL			=	ExtractIkBone( "LHand", "LMiddle1", handUpL );
			handR			=	ExtractIkBone( "RHand", "RMiddle1", handUpR );

			//	get local joint positions :
			headJoint		=	SkinningData.Bones["Head"].LocalBindPose.TranslationVector;
			chestJoint		=	SkinningData.Bones["Chest"].LocalBindPose.TranslationVector;
			hipJointL		=	SkinningData.Bones["LHip"].LocalBindPose.TranslationVector;
			hipJointR		=	SkinningData.Bones["RHip"].LocalBindPose.TranslationVector;
			armJointL		=	SkinningData.Bones["LArm"].LocalBindPose.TranslationVector;
			armJointR		=	SkinningData.Bones["RArm"].LocalBindPose.TranslationVector;

			fingersL.thumb	=	SkinningData.Bones["LThumb1"].LocalBindPose.TranslationVector;
			fingersL.index	=	SkinningData.Bones["LIndex1"].LocalBindPose.TranslationVector;
			fingersL.middle	=	SkinningData.Bones["LMiddle1"].LocalBindPose.TranslationVector;
			fingersL.ring	=	SkinningData.Bones["LRing1"].LocalBindPose.TranslationVector;
			fingersL.pinky	=	SkinningData.Bones["LPinky1"].LocalBindPose.TranslationVector;

			fingersR.thumb	=	SkinningData.Bones["RThumb1"].LocalBindPose.TranslationVector;
			fingersR.index	=	SkinningData.Bones["RIndex1"].LocalBindPose.TranslationVector;
			fingersR.middle	=	SkinningData.Bones["RMiddle1"].LocalBindPose.TranslationVector;
			fingersR.ring	=	SkinningData.Bones["RRing1"].LocalBindPose.TranslationVector;
			fingersR.pinky	=	SkinningData.Bones["RPinky1"].LocalBindPose.TranslationVector;

			fingersL.thumbChain		=	new IkBoneChain3( this, "LThumb1",  "LThumb2",  "LThumb3",  "LThumbTip",  handUpL );
			fingersL.indexChain		=	new IkBoneChain3( this, "LIndex1",  "LIndex2",  "LIndex3",  "LIndexTip",  handUpL );
			fingersL.middleChain	=	new IkBoneChain3( this, "LMiddle1", "LMiddle2", "LMiddle3", "LMiddleTip", handUpL );
			fingersL.ringChain		=	new IkBoneChain3( this, "LRing1",   "LRing2",   "LRing3",   "LRingTip",   handUpL );
			fingersL.pinkyChain		=	new IkBoneChain3( this, "LPinky1",  "LPinky2",  "LPinky3",  "LPinkyTip",  handUpL );

			fingersR.thumbChain		=	new IkBoneChain3( this, "RThumb1",  "RThumb2",  "RThumb3",  "RThumbTip",  handUpR );
			fingersR.indexChain		=	new IkBoneChain3( this, "RIndex1",  "RIndex2",  "RIndex3",  "RIndexTip",  handUpR );
			fingersR.middleChain	=	new IkBoneChain3( this, "RMiddle1", "RMiddle2", "RMiddle3", "RMiddleTip", handUpR );
			fingersR.ringChain		=	new IkBoneChain3( this, "RRing1",   "RRing2",   "RRing3",   "RRingTip",   handUpR );
			fingersR.pinkyChain		=	new IkBoneChain3( this, "RPinky1",  "RPinky2",  "RPinky3",  "RPinkyTip",  handUpR );

			ComputeMetrics();

			//	setup linked IK particles for FBIK :
			ikPrts			=	new IkParticleSystem( (int)PrtIdx.Max );
			ikPrts.AddLink( (int)PrtIdx.Chest,	(int)PrtIdx.LHip,	GlobalBoneToBoneVector( "Chest", "LHip" ).Length() );
			ikPrts.AddLink( (int)PrtIdx.Chest,	(int)PrtIdx.RHip,	GlobalBoneToBoneVector( "Chest", "RHip" ).Length() );
			ikPrts.AddLink( (int)PrtIdx.Chest,	(int)PrtIdx.LArm,	GlobalBoneToBoneVector( "Chest", "LArm" ).Length() );
			ikPrts.AddLink( (int)PrtIdx.Chest,	(int)PrtIdx.RArm,	GlobalBoneToBoneVector( "Chest", "RArm" ).Length() );
			ikPrts.AddLink( (int)PrtIdx.LHip,	(int)PrtIdx.RHip,	GlobalBoneToBoneVector( "LHip",  "RHip" ).Length() );
			ikPrts.AddLink( (int)PrtIdx.LArm,	(int)PrtIdx.RArm,	GlobalBoneToBoneVector( "LArm",  "RArm" ).Length() );

			float armHipDistL = base.GlobalBoneToBoneVector( "LArm",  "LHip" ).Length();
			float armHipDistR = base.GlobalBoneToBoneVector( "RArm",  "RHip" ).Length();
			ikPrts.AddLink( (int)PrtIdx.LArm,	(int)PrtIdx.LHip,	armHipDistL * 0.8f,	armHipDistL * 1.2f );
			ikPrts.AddLink( (int)PrtIdx.RArm,	(int)PrtIdx.RHip,	armHipDistR * 0.8f,	armHipDistR * 1.2f );
		}



		void ComputeMetrics ()
		{
			HumanHeight		=	Vector3.Dot( Vector3.Up, SkinningData.Bones["Head"].BindPose.TranslationVector );
			ShoulderWidth	=	GlobalBoneToBoneVector( "LArm",		"RArm"		).Length();
			ShoulderWidth	=	GlobalBoneToBoneVector( "LHip",		"RHip"		).Length();
			LPalmWidth		=	GlobalBoneToBoneVector( "LIndex1",	"LPinky1"	).Length();
			RPalmWidth		=	GlobalBoneToBoneVector( "RIndex1",	"RPinky1"	).Length();
		}



		Matrix ComputeTransformByThreePoints ( Vector3 p0, Vector3 p1, Vector3 p2, Vector3 q0, Vector3 q1, Vector3 q2 )
		{
			Matrix P	=	AimBasis( p1 - p0, Vector3.Cross( p1-p0, p2-p0 ), p0 );
			Matrix Q	=	AimBasis( q1 - q0, Vector3.Cross( q1-q0, q2-q0 ), q0 );
			Matrix T	=	Matrix.Invert( P ) * Q;

			return T;
		}



		public void Solve ( IkHumanTarget target )
		{
			var pelvisMatrix		=	pelvis.Aim	( target.PelvisBasis.TranslationVector, target.PelvisBasis.Forward, target.PelvisBasis.Up, this );

			var globalChestJoint	=	pelvis.GetGlobalPoint( chestJoint );

			var chestMatrix			=	chest.Aim	( globalChestJoint,  target.ChestBasis.Forward,  target.ChestBasis.Up,  this );

			var globalHipJointL		=	pelvis.GetGlobalPoint( hipJointL );
			var globalHipJointR		=	pelvis.GetGlobalPoint( hipJointR );
			var globalArmJointL		=	chest.GetGlobalPoint( armJointL );
			var globalArmJointR		=	chest.GetGlobalPoint( armJointR );
			var globalAncleL		=	legL.AnclePosition( target.LFootPrint );
			var globalAncleR		=	legR.AnclePosition( target.RFootPrint );

			//
			//	Solve FBIK :
			//
			ikPrts.SetParticle( (int)PrtIdx.Chest,	globalChestJoint );
			ikPrts.SetParticle( (int)PrtIdx.LHip,	globalHipJointL	 );
			ikPrts.SetParticle( (int)PrtIdx.RHip,	globalHipJointR	 );
			ikPrts.SetParticle( (int)PrtIdx.LArm,	globalArmJointL	 );
			ikPrts.SetParticle( (int)PrtIdx.RArm,	globalArmJointR	 );

			ikPrts.AddExtLink( (int)PrtIdx.LHip,	globalAncleL, legL.MinDistance() * 1.2f, 0.95f * legL.MaxDistance() );
			ikPrts.AddExtLink( (int)PrtIdx.RHip,	globalAncleR, legR.MinDistance() * 1.2f, 0.95f * legR.MaxDistance() );

			ikPrts.AddExtLink( (int)PrtIdx.LArm,	target.LHand.TranslationVector, armL.MinDistance() * 1.2f, 0.95f * armL.MaxDistance() );
			ikPrts.AddExtLink( (int)PrtIdx.RArm,	target.RHand.TranslationVector, armR.MinDistance() * 1.2f, 0.95f * armR.MaxDistance() );
							   
			ikPrts.Solve( 40, target.PelvisBasis.Backward/100.0f );
			ikPrts.Draw( dr );

			var newGlobalChestJoint	=	ikPrts.GetParticle( (int)PrtIdx.Chest );
			var newGlobalHipJointL	=	ikPrts.GetParticle( (int)PrtIdx.LHip );
			var newGlobalHipJointR	=	ikPrts.GetParticle( (int)PrtIdx.RHip );
			var newGlobalArmJointL	=	ikPrts.GetParticle( (int)PrtIdx.LArm );
			var newGlobalArmJointR	=	ikPrts.GetParticle( (int)PrtIdx.RArm );


			var updatePelvisMatrix	=	ComputeTransformByThreePoints(  globalChestJoint,    globalHipJointL,    globalHipJointR,	
																		newGlobalChestJoint, newGlobalHipJointL, newGlobalHipJointR );	
			
			var updateChestMatrix	=	ComputeTransformByThreePoints(  globalChestJoint,    globalArmJointL,    globalArmJointR,	
																		newGlobalChestJoint, newGlobalArmJointL, newGlobalArmJointR );	
			
			pelvis.SetGlobalMatrix( pelvisMatrix * updatePelvisMatrix, this );
			chest.SetGlobalMatrix( chestMatrix * updateChestMatrix, this );

			//
			//	Solve head :
			//
			var globalHeadJoint = Vector3.TransformCoordinate( headJoint, chest.GlobalMatrix );
			var headMatrix = head.Aim( globalHeadJoint, target.HeadLookTarget - globalHeadJoint, target.HeadUp, this );
			dr.DrawLine( globalHeadJoint, target.HeadLookTarget, new Color(255,255,255,64) );
			dr.DrawPoint( globalHeadJoint, 0.1f, Color.White );

			
			//
			//	Solve legs and arms :
			//
			legL.Solve( newGlobalHipJointL, target.LFootPrint, target.Basis.Forward ); 
			legR.Solve( newGlobalHipJointR, target.RFootPrint, target.Basis.Forward ); 

			var las = armL.Solve( newGlobalArmJointL, target.LHand.TranslationVector, target.Basis.Backward ); 
			var ras = armR.Solve( newGlobalArmJointR, target.RHand.TranslationVector, target.Basis.Backward ); 

			var handLMatrix	=	handL.Aim( las.hitPos, target.LHand.Forward, target.LHand.Up, this );
			var handRMatrix	=	handR.Aim( ras.hitPos, target.RHand.Forward, target.RHand.Up, this );

			var thumbUpL	=	target.LHand.Up + target.LHand.Backward + target.LHand.Right;
			fingersL.thumbChain	.Solve( Vector3.Transform( fingersL.thumb	, handLMatrix ), target.LFingers.Thumb	, thumbUpL );
			fingersL.indexChain	.Solve( Vector3.Transform( fingersL.index	, handLMatrix ), target.LFingers.Index	, target.LHand.Up );
			fingersL.middleChain.Solve( Vector3.Transform( fingersL.middle	, handLMatrix ), target.LFingers.Middle	, target.LHand.Up );
			fingersL.ringChain	.Solve( Vector3.Transform( fingersL.ring	, handLMatrix ), target.LFingers.Ring	, target.LHand.Up );
			fingersL.pinkyChain	.Solve( Vector3.Transform( fingersL.pinky	, handLMatrix ), target.LFingers.Pinky	, target.LHand.Up );

			var thumbUpR	=	target.RHand.Up + target.RHand.Backward + target.RHand.Left;
			fingersR.thumbChain	.Solve( Vector3.Transform( fingersR.thumb	, handRMatrix ), target.RFingers.Thumb	, thumbUpR );
			fingersR.indexChain	.Solve( Vector3.Transform( fingersR.index	, handRMatrix ), target.RFingers.Index	, target.RHand.Up );
			fingersR.middleChain.Solve( Vector3.Transform( fingersR.middle	, handRMatrix ), target.RFingers.Middle	, target.RHand.Up );
			fingersR.ringChain	.Solve( Vector3.Transform( fingersR.ring	, handRMatrix ), target.RFingers.Ring	, target.RHand.Up );
			fingersR.pinkyChain	.Solve( Vector3.Transform( fingersR.pinky	, handRMatrix ), target.RFingers.Pinky	, target.RHand.Up );

						
			//base.TransformChildren( "LForearm", las.bone1 );
			//base.TransformChildren( "RForearm", ras.bone1 );

			GenerateSkinningTransforms();
		}


	}
}
