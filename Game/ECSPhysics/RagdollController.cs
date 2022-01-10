using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using IronStar.Animation;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BepuEntity = BEPUphysics.Entities.Entity;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints.SolverGroups;
using BEPUphysics.Constraints.TwoEntity.JointLimits;
using BEPUphysics.CollisionRuleManagement;
using Fusion;

namespace IronStar.ECSPhysics
{
	public class RagdollController
	{
		readonly PhysicsCore physics;
		readonly Scene scene;
		readonly BipedMapping mapping;

		Matrix[] bindPose;

		readonly List<RagdollBone>	ragdollBones = new List<RagdollBone>(32);
		readonly List<ISpaceObject>	physObjects	 = new List<ISpaceObject>(32);


		public RagdollController( PhysicsCore physics, Scene scene )
		{
			this.physics	=	physics;	
			this.scene		=	scene;
			mapping			=	new BipedMapping(scene);

			bindPose		=	scene.GetBindPose();

			//--------------------------
			//	Arms :
			//--------------------------
			var leftShldr	=	mapping.GetLimbCapsule( mapping.LeftShoulder,	mapping.LeftArm		, 0.30f);
			var leftArm		=	mapping.GetLimbCapsule( mapping.LeftArm,		mapping.LeftHand	, 0.25f);

			var rightShldr	=	mapping.GetLimbCapsule( mapping.RightShoulder,	mapping.RightArm	, 0.30f);
			var rightArm	=	mapping.GetLimbCapsule( mapping.RightArm,		mapping.RightHand	, 0.25f);
			var handR		=	mapping.GetBoneBox( mapping.RightHand, null, 1 );
			var handL		=	mapping.GetBoneBox( mapping.LeftHand, null, 1 );

			ragdollBones.Add( leftShldr	 );
			ragdollBones.Add( leftArm	 );
			ragdollBones.Add( rightShldr );
			ragdollBones.Add( rightArm	 );
			ragdollBones.Add( handR		 );
			ragdollBones.Add( handL		 );

			ConnectKneeElbow( leftShldr,  leftArm,  Vector3.Left );
			ConnectKneeElbow( rightShldr, rightArm, Vector3.Left );

			ConnectBallSocket( leftArm,  handL, Vector3.Down, 45, 30 );
			ConnectBallSocket( rightArm, handR, Vector3.Down, 45, 30 );	 //*/

			//--------------------------
			//	Legs :
			//--------------------------
			var hipL	=	mapping.GetLimbCapsule( mapping.LeftHip,		mapping.LeftShin	, 0.40f );
			var shinL	=	mapping.GetLimbCapsule( mapping.LeftShin,		mapping.LeftFoot	, 0.28f );

			var hipR	=	mapping.GetLimbCapsule( mapping.RightHip,		mapping.RightShin	, 0.40f );
			var shinR	=	mapping.GetLimbCapsule( mapping.RightShin,		mapping.RightFoot	, 0.28f );

			ConnectKneeElbow( hipL, shinL, Vector3.Right );
			ConnectKneeElbow( hipR, shinR, Vector3.Right );

			var footL = mapping.GetBoneBox( mapping.LeftFoot,	mapping.LeftToe,  1 );
			var footR = mapping.GetBoneBox( mapping.RightFoot,	mapping.RightToe, 1 );

			ConnectBallSocket( shinL, footL, Vector3.Down, 25, 25 );
			ConnectBallSocket( shinR, footR, Vector3.Down, 25, 25 );
			
			ragdollBones.Add( hipL	);
			ragdollBones.Add( hipR	);
			ragdollBones.Add( shinL	);
			ragdollBones.Add( shinR	);
			ragdollBones.Add( footL	);
			ragdollBones.Add( footR	);

			//--------------------------
			//	Torso :
			//--------------------------
			var chest	=	mapping.GetBoneBox( mapping.Chest,	null,	5 );
			var spine1	=	mapping.GetBoneBox( mapping.Spine1,	null,	2 );
			var spine2	=	mapping.GetBoneBox( mapping.Spine2,	null,	2 );
			var pelvis	=	mapping.GetBoneBox( mapping.Pelvis,	null,	4 );
			var head	=	mapping.GetBoneBox( mapping.Head,	null,	3 );

			ConnectBallSocket( pelvis, spine1,	Vector3.Up, 10, 15 );
			ConnectBallSocket( spine1, spine2,	Vector3.Up, 10, 15 );
			ConnectBallSocket( spine2, chest,	Vector3.Up, 10, 15 );
			ConnectBallSocket( chest, head,		Vector3.Up, 10, 45 ); //*/

			ragdollBones.Add( chest	 );
			ragdollBones.Add( spine1 );
			ragdollBones.Add( spine2 );
			ragdollBones.Add( pelvis );
			ragdollBones.Add( head	 );

			//--------------------------
			//	Torso + Limbs :
			//--------------------------
			ConnectBallSocket( pelvis, hipL, 0.3f*Vector3.Down - Vector3.Left,  20, 45 );
			ConnectBallSocket( pelvis, hipR, 0.3f*Vector3.Down - Vector3.Right, 20, 45 );

			ConnectBallSocket( chest, leftShldr,  Vector3.Left,  10, 45 );
			ConnectBallSocket( chest, rightShldr, Vector3.Right, 10, 45 ); //*/

			//--------------------------
			//	Add ragdoll bones to 
			//	physics world
			//--------------------------
			foreach (var bone in ragdollBones)
			{
				bone.PhysEntity.CollisionInformation.CollisionRules.Group = physics.RagdollGroup;
				Add( bone.PhysEntity );
			}
		}


		void ConnectBallSocket( RagdollBone a, RagdollBone b, Vector3 axis, float angle, float maxTwist )
		{
			var pos = MathConverter.Convert( b.Node.BindPose.TranslationVector );
			var ballSocketJoint = new BallSocketJoint(a.PhysEntity, b.PhysEntity, pos);
			Add(ballSocketJoint);

			var angularMotor = new BEPUphysics.Constraints.TwoEntity.Motors.AngularMotor(a.PhysEntity, b.PhysEntity);
			angularMotor.Settings.MaximumForce = 250;
			angularMotor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.VelocityMotor;
			Add(angularMotor);		

			var angleRad	= MathUtil.DegreesToRadians(angle);
			var twistRad	= MathUtil.DegreesToRadians(maxTwist);
			var swingLimit	= new EllipseSwingLimit(a.PhysEntity, b.PhysEntity, MathConverter.Convert(axis), angleRad, angleRad);

			var bpaxis = MathConverter.Convert(axis);

			//var twistLimit	= new TwistLimit(a.PhysEntity, b.PhysEntity, bpaxis, bpaxis, -twistRad, twistRad);

			//Add(twistLimit);
			Add(swingLimit);

			CollisionRules.AddRule( a.PhysEntity, b.PhysEntity, CollisionRule.NoSolver );
		}

		
		void ConnectKneeElbow( RagdollBone a, RagdollBone b, Vector3 axis )
		{
			BEPUutilities.Quaternion relative;
			var qa = a.PhysEntity.Orientation;
			var qb = b.PhysEntity.Orientation;
			BEPUutilities.Quaternion.GetRelativeRotation( ref qa, ref qb, out relative );
			float angle = MathConverter.Convert( relative ).Angle;

			//var joint = new BallSocketJoint( a, b, MathConverter.Convert( node.BindPose.TranslationVector ) );
			var joint = new SwivelHingeJoint( a.PhysEntity, b.PhysEntity, MathConverter.Convert( b.Node.BindPose.TranslationVector ), MathConverter.Convert( axis ) );

			joint.TwistLimit.IsActive = true;
			joint.TwistLimit.MinimumAngle = -MathUtil.PiOverFour / 8;
			joint.TwistLimit.MaximumAngle = +MathUtil.PiOverFour / 8;
			joint.TwistLimit.SpringSettings.Damping		= 100;
			joint.TwistLimit.SpringSettings.Stiffness	= 100;

			//The joint is like a hinge, but it can't hyperflex.
			joint.HingeLimit.IsActive = true;
			joint.HingeLimit.MinimumAngle = 0;
			joint.HingeLimit.MaximumAngle = MathUtil.Pi * 0.7f;
			//joint.HingeLimit.MaximumAngle = MathUtil.Pi * 0.9f;
			/*joint.HingeLimit.SpringSettings.Damping  = 100;
			joint.HingeLimit.SpringSettings.Stiffness  = 100;*/
			joint.HingeMotor.IsActive  = true;
			joint.HingeMotor.Settings.VelocityMotor.Softness = 500f;
			joint.HingeMotor.Settings.Mode =  BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.VelocityMotor;

			/*var angularMotor = new BEPUphysics.Constraints.TwoEntity.Motors.AngularMotor(a.PhysEntity, b.PhysEntity);
			angularMotor.Settings.MaximumForce = 20;
			angularMotor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.VelocityMotor;
			Add( angularMotor );//*/

			CollisionRules.AddRule( a.PhysEntity, b.PhysEntity, CollisionRule.NoSolver );

			Add( joint );
		}


		void Add( ISpaceObject obj )
		{
			physics.Add( obj );
			physObjects.Add( obj );
		}



		public void LoadAnimatedTransforms( ECS.Transform transform, BoneComponent bones )
		{
			foreach ( var ragdollBone in ragdollBones )
			{
				ragdollBone.LoadAnimatedTransforms( transform.TransformMatrix, transform.LinearVelocity, bones.Bones );
			}
		}


		public void ApplyTransforms( ECS.Transform transform, BoneComponent bones )
		{
			var dr = physics.Game.RenderSystem.RenderWorld.Debug.Async;

			var worldInv = Matrix.Invert( transform.TransformMatrix );

			foreach ( var ragdollBone in ragdollBones )
			{
				ragdollBone.ApplySimulatedTransform( worldInv, bones.Bones );
			}
		}

		public void ApplyInitialImpulse( ImpulseComponent impulse )
		{
			if (impulse==null) return;
			if (impulse.Impulse.Length()<0.001f) return;

			Plane plane = new Plane( impulse.Location, impulse.Impulse.Normalized() );

			RagdollBone closestBone = null;
			float minDistance = 9999999;
			
			foreach ( var ragdollBone in ragdollBones )
			{
				var bonePos	 = MathConverter.Convert( ragdollBone.PhysEntity.MotionState.Position );
				var projPos  = ProjectOnPlane( plane, bonePos );
				var distance = Vector3.Distance( impulse.Location, projPos );

				if (distance<minDistance)
				{
					minDistance = distance;
					closestBone = ragdollBone;
				}
			}

			//Log.Warning("RAGDOLL IMPULSE : {0} {1} {2}", closestBone.Node.Name, impulse.Location, impulse.Impulse );

			var impulseMagnitude	=	ClampVector( impulse.Impulse, 300, 1000 );
			var impulseLocation		=	impulse.Location;

			closestBone?.PhysEntity?.ApplyImpulse( MathConverter.Convert( impulseLocation ), MathConverter.Convert( impulseMagnitude ) );
		}


		Vector3 ClampVector( Vector3 vector, float min, float max )
		{
			var length = MathUtil.Clamp( vector.Length(), min, max );
			return vector.Normalized() * length;
		}


		Vector3 ProjectOnPlane( Plane plane, Vector3 point )
		{
			float dist = Plane.DotCoordinate( plane, point );
			return point - plane.Normal * dist;
		}


		public void Destroy()
		{
			foreach ( var obj in physObjects )
			{
				physics.Remove( obj );
			}
		}
	}
}
