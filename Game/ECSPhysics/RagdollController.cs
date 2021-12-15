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
		readonly float scale;

		Matrix[] bindPose;

		readonly List<ISpaceObject> objects = new List<ISpaceObject>();


		public RagdollController( PhysicsCore physics, Scene scene, float scale )
		{
			this.physics	=	physics;	
			this.scene		=	scene;
			this.scale		=	scale;
			mapping			=	new BipedMapping(scene, scale);

			bindPose		=	scene.GetBindPose();

			//--------------------------
			//	Arms :
			//--------------------------
			var leftShldr	=	AddCapsule( mapping.GetLimbCapsule( mapping.LeftShoulder,	mapping.LeftArm		, 1.5f / 2.7f) );
			var leftArm		=	AddCapsule( mapping.GetLimbCapsule( mapping.LeftArm,		mapping.LeftHand	, 1.5f / 2.7f) );

			var rightShldr	=	AddCapsule( mapping.GetLimbCapsule( mapping.RightShoulder,	mapping.RightArm	, 1.5f / 2.9f) );
			var rightArm	=	AddCapsule( mapping.GetLimbCapsule( mapping.RightArm,		mapping.RightHand	, 1.5f / 2.9f) );
			var handR		=	AddBox( mapping.GetBoneBox( mapping.RightHand, null, 1 ) );
			var handL		=	AddBox( mapping.GetBoneBox( mapping.LeftHand, null, 1 ) );

			ConnectKneeElbow( leftShldr,  leftArm,  mapping.LeftArm , Vector3.Right );
			ConnectKneeElbow( rightShldr, rightArm, mapping.RightArm, Vector3.Right );

			ConnectBallSocket( leftArm,  handL, mapping.LeftHand,  Vector3.Down, 45 );
			ConnectBallSocket( rightArm, handR, mapping.RightHand, Vector3.Down, 45 );	 //*/

			//--------------------------
			//	Legs :
			//--------------------------
			var hipL	=	AddCapsule( mapping.GetLimbCapsule( mapping.LeftHip,		mapping.LeftShin	, 1.0f / 2.4f) );
			var shinL	=	AddCapsule( mapping.GetLimbCapsule( mapping.LeftShin,		mapping.LeftFoot	, 1.0f / 2.8f) );

			var hipR	=	AddCapsule( mapping.GetLimbCapsule( mapping.RightHip,		mapping.RightShin	, 1.0f / 2.4f) );
			var shinR	=	AddCapsule( mapping.GetLimbCapsule( mapping.RightShin,		mapping.RightFoot	, 1.0f / 2.8f) );

			ConnectKneeElbow( hipL, shinL, mapping.LeftShin , Vector3.Right );
			ConnectKneeElbow( hipR, shinR, mapping.RightShin, Vector3.Right );

			var footL = AddBox( mapping.GetBoneBox( mapping.LeftFoot,	mapping.LeftToe,  1 ) );
			var footR = AddBox( mapping.GetBoneBox( mapping.RightFoot,	mapping.RightToe, 1 ) );

			ConnectBallSocket( shinL, footL, mapping.LeftFoot,  Vector3.Down, 45 );
			ConnectBallSocket( shinR, footR, mapping.RightFoot, Vector3.Down, 45 );		//*/
			
			//--------------------------
			//	Torso :
			//--------------------------
			var chest	=	AddBox( mapping.GetBoneBox( mapping.Chest,	null,	5 ) );
			var spine1	=	AddBox( mapping.GetBoneBox( mapping.Spine1,	null,	2 ) );
			var spine2	=	AddBox( mapping.GetBoneBox( mapping.Spine2,	null,	2 ) );
			var pelvis	=	AddBox( mapping.GetBoneBox( mapping.Pelvis,	null,	4 ) );
			var head	=	AddBox( mapping.GetBoneBox( mapping.Head,	null,	3 ) );

			ConnectBallSocket( pelvis, spine1,	mapping.Spine1,	Vector3.Up, 30 );
			ConnectBallSocket( spine1, spine2,	mapping.Spine2,	Vector3.Up, 30 );
			ConnectBallSocket( spine2, chest,	mapping.Chest,	Vector3.Up, 30 );
			ConnectBallSocket( chest, head,		mapping.Head,	Vector3.Up, 30 ); //*/

			//--------------------------
			//	Troso + Limbs :
			//--------------------------
			ConnectBallSocket( pelvis, hipL, mapping.LeftHip,  Vector3.Down - Vector3.Left,  90 );
			ConnectBallSocket( pelvis, hipR, mapping.RightHip, Vector3.Down - Vector3.Right, 90 );

			ConnectBallSocket( chest, leftShldr,  mapping.LeftShoulder,  Vector3.Left,  90 );
			ConnectBallSocket( chest, rightShldr, mapping.RightShoulder, Vector3.Right, 90 ); //*/

			chest.ApplyImpulse( chest.Position, MathConverter.Convert( Vector3.Right + Vector3.ForwardRH ) * 400 );
		}


		void ConnectBallSocket( BepuEntity a, BepuEntity b, Node node, Vector3 axis, float angle )
		{
			var pos = MathConverter.Convert( node.BindPose.TranslationVector * scale );
			var ballSocketJoint = new BallSocketJoint(a, b, pos);
			Add(ballSocketJoint);

			var angularMotor = new BEPUphysics.Constraints.TwoEntity.Motors.AngularMotor(a, b);
			angularMotor.Settings.MaximumForce = 50;
			angularMotor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.VelocityMotor;
			Add(angularMotor);

			var angleRad = MathUtil.DegreesToRadians(angle);

			//Shoulders don't have a simple limit.  The EllipseSwingLimit allows angles within an ellipse, which is closer to how some joints function
			//than just flat planes (like two RevoluteLimits) or a single angle (like SwingLimits).
			var swingLimit = new EllipseSwingLimit(a, b, MathConverter.Convert(axis), angleRad, angleRad);
			Add(swingLimit);
		}

		void ConnectKneeElbow( BepuEntity a, BepuEntity b, Node node, Vector3 axis )
		{
			BEPUutilities.Quaternion relative;
			var qa = a.Orientation;
			var qb = b.Orientation;
			BEPUutilities.Quaternion.GetRelativeRotation( ref qa, ref qb, out relative );
			float angle = MathConverter.Convert( relative ).Angle;

			//var joint = new BallSocketJoint( a, b, MathConverter.Convert( node.BindPose.TranslationVector ) );
			var joint = new SwivelHingeJoint( a, b, MathConverter.Convert( node.BindPose.TranslationVector * scale ), MathConverter.Convert( axis ) );

			joint.TwistLimit.IsActive = true;
			joint.TwistLimit.MinimumAngle = angle - MathUtil.PiOverFour / 2;
			joint.TwistLimit.MaximumAngle = angle + MathUtil.PiOverFour / 2;
			joint.TwistLimit.SpringSettings.Damping = 100;
			joint.TwistLimit.SpringSettings.Advanced.Softness = 0.5f;

			//The joint is like a hinge, but it can't hyperflex.
			joint.HingeLimit.IsActive = true;
			joint.HingeLimit.MinimumAngle = 0;
			joint.HingeLimit.MaximumAngle = MathUtil.Pi * .9f;

			var angularMotor = new BEPUphysics.Constraints.TwoEntity.Motors.AngularMotor(a, b);
			angularMotor.Settings.MaximumForce = 20;
			angularMotor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.VelocityMotor;
			Add(angularMotor);


			Add( joint );
		}


		Capsule AddCapsule( Capsule capsule )
		{
			capsule.CollisionInformation.CollisionRules.Group =	physics.GetCollisionGroup(CollisionGroup.Ragdoll);
			physics.Add( capsule );
			objects.Add( capsule );
			return capsule;
		}


		void Add( ISpaceObject obj )
		{
			physics.Add( obj );
			objects.Add( obj );
		}


		Box AddBox( Box box )
		{
			box.CollisionInformation.CollisionRules.Group =	physics.GetCollisionGroup(CollisionGroup.Ragdoll);
			physics.Add( box );
			objects.Add( box );
			return box;
		}


		public void DrawDebug( DebugRender dr )
		{
			foreach ( var bt in bindPose )
			{
				dr.DrawBasis( bt, 0.3f, 1 );
			}

			/*
			mapping.DrawLimbCapsule( dr, mapping.LeftShoulder,	mapping.LeftArm		);
			mapping.DrawLimbCapsule( dr, mapping.RightShoulder,	mapping.RightArm	);

			mapping.DrawLimbCapsule( dr, mapping.LeftArm,		mapping.LeftHand	);
			mapping.DrawLimbCapsule( dr, mapping.RightArm,		mapping.RightHand	);

			mapping.DrawLimbCapsule( dr, mapping.LeftHip,		mapping.LeftShin	);
			mapping.DrawLimbCapsule( dr, mapping.LeftShin,		mapping.LeftFoot	);
			mapping.DrawLimbCapsule( dr, mapping.LeftFoot,		mapping.LeftToe		);

			mapping.DrawLimbCapsule( dr, mapping.RightHip,		mapping.RightShin	);
			mapping.DrawLimbCapsule( dr, mapping.RightShin,		mapping.RightFoot	);
			mapping.DrawLimbCapsule( dr, mapping.RightFoot,		mapping.RightToe	);
			*/
		}


		public void Destroy()
		{
			foreach ( var obj in objects )
			{
				physics.Remove( obj );
			}
		}
	}
}
