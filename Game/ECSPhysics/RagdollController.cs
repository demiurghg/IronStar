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

			ConnectKneeElbow( leftShldr,  leftArm,  Vector3.Right );
			ConnectKneeElbow( rightShldr, rightArm, Vector3.Right );

			ConnectBallSocket( leftArm,  handL, Vector3.Down, 45 );
			ConnectBallSocket( rightArm, handR, Vector3.Down, 45 );	 //*/

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

			ConnectBallSocket( shinL, footL, Vector3.Down, 45 );
			ConnectBallSocket( shinR, footR, Vector3.Down, 45 );		//*/
			
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

			ConnectBallSocket( pelvis, spine1,	Vector3.Up, 30 );
			ConnectBallSocket( spine1, spine2,	Vector3.Up, 30 );
			ConnectBallSocket( spine2, chest,	Vector3.Up, 30 );
			ConnectBallSocket( chest, head,		Vector3.Up, 30 ); //*/

			ragdollBones.Add( chest	 );
			ragdollBones.Add( spine1 );
			ragdollBones.Add( spine2 );
			ragdollBones.Add( pelvis );
			ragdollBones.Add( head	 );

			//--------------------------
			//	Torso + Limbs :
			//--------------------------
			ConnectBallSocket( pelvis, hipL, Vector3.Down - Vector3.Left,  90 );
			ConnectBallSocket( pelvis, hipR, Vector3.Down - Vector3.Right, 90 );

			ConnectBallSocket( chest, leftShldr,  Vector3.Left,  90 );
			ConnectBallSocket( chest, rightShldr, Vector3.Right, 90 ); //*/

			chest.PhysEntity.ApplyImpulse( chest.PhysEntity.Position, MathConverter.Convert( Vector3.Right + Vector3.ForwardRH ) * 400 );

			//--------------------------
			//	Add ragdoll bones to 
			//	physics world
			//--------------------------
			foreach (var bone in ragdollBones)
			{
				Add( bone.PhysEntity );
			}
		}


		void ConnectBallSocket( RagdollBone a, RagdollBone b, Vector3 axis, float angle )
		{
			var pos = MathConverter.Convert( b.Node.BindPose.TranslationVector );
			var ballSocketJoint = new BallSocketJoint(a.PhysEntity, b.PhysEntity, pos);
			Add(ballSocketJoint);

			var angularMotor = new BEPUphysics.Constraints.TwoEntity.Motors.AngularMotor(a.PhysEntity, b.PhysEntity);
			angularMotor.Settings.MaximumForce = 50;
			angularMotor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.VelocityMotor;
			Add(angularMotor);

			var angleRad	= MathUtil.DegreesToRadians(angle);
			var swingLimit	= new EllipseSwingLimit(a.PhysEntity, b.PhysEntity, MathConverter.Convert(axis), angleRad, angleRad);

			Add(swingLimit);
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
			joint.TwistLimit.MinimumAngle = angle - MathUtil.PiOverFour / 2;
			joint.TwistLimit.MaximumAngle = angle + MathUtil.PiOverFour / 2;
			joint.TwistLimit.SpringSettings.Damping = 100;
			joint.TwistLimit.SpringSettings.Advanced.Softness = 0.5f;

			//The joint is like a hinge, but it can't hyperflex.
			joint.HingeLimit.IsActive = true;
			joint.HingeLimit.MinimumAngle = 0;
			joint.HingeLimit.MaximumAngle = MathUtil.Pi * .9f;

			var angularMotor = new BEPUphysics.Constraints.TwoEntity.Motors.AngularMotor(a.PhysEntity, b.PhysEntity);
			angularMotor.Settings.MaximumForce = 20;
			angularMotor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.VelocityMotor;

			Add( angularMotor );
			Add( joint );
		}


		void Add( ISpaceObject obj )
		{
			physics.Add( obj );
			physObjects.Add( obj );
		}


		public void ApplyTransforms( ECS.Transform transform, BoneComponent bones )
		{
			var dr = physics.Game.RenderSystem.RenderWorld.Debug.Async;

			foreach ( var node in scene.Nodes )
			{
				dr.DrawBasis( node.BindPose, 0.5f, 4 );
			}

			var world = transform.TransformMatrix;

			foreach ( var ragdollBone in ragdollBones )
			{
				bones.Bones[ ragdollBone.Index ] = ragdollBone.ComputePhysicalBoneTransform(world);

				dr.DrawBasis( ragdollBone.ComputePhysicalBoneTransform(world), 3.5f, 4 );
			}
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
