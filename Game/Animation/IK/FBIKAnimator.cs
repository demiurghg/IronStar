using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using Fusion.Scripting;
using KopiLua;
using IronStar.Animation;
using BEPUik;

namespace IronStar.Animation.IK
{
	public class FBIKAnimator
	{
		readonly Scene	scene;
		readonly Matrix modelTransform;

		public Effector Head		;
		public Effector Chest		;
		public Effector Pelvis		;
		public Effector LeftHand	;
		public Effector RightHand	;
		public Effector LeftFoot	;
		public Effector RightFoot	;
		public Effector LeftToe		;
		public Effector RightToe	;

		Matrix[]	globalPose;
		BEPUutilities.Vector3[]		globalTranslation;
		BEPUutilities.Quaternion[]	globalRotation;
		IKSolver	ikSolver;

		Bone[]			bones;
		List<IKJoint>	joints;
		List<Control>	controls;

		Bone		pelvis;
		Bone		spine1;
		Bone		spine2;
		Bone		chest;
		Bone		head;
		
		Bone		L_hip;
		Bone		L_shin;
		Bone		L_foot;
		Bone		L_toe;
		
		Bone		R_hip;
		Bone		R_shin;
		Bone		R_foot;
		Bone		R_toe;

		Bone		L_shoulder;
		Bone		L_arm;
		Bone		L_hand;

		Bone		R_shoulder;
		Bone		R_arm;
		Bone		R_hand;

		DragControl	R_handEffector;


		/// <summary>
		/// 
		/// </summary>
		public FBIKAnimator( Scene scene, Matrix modelTransform )
		{
			this.modelTransform	=	modelTransform;
			this.scene			=	scene;

			//	compute global pose
			globalPose	=	new Matrix[ scene.Nodes.Count ];
			scene.ComputeAbsoluteTransforms( globalPose );

			globalTranslation	=	new BEPUutilities.Vector3[ scene.Nodes.Count ];
			globalRotation		=	new BEPUutilities.Quaternion[ scene.Nodes.Count ];

			//	scale global pose to fit view model :
			for (int i=0; i<globalPose.Length; i++)
			{
				globalPose[i]			=	globalPose[i];
				globalTranslation[i]	=	MathConverter.Convert( globalPose[i].TranslationVector );
				globalRotation[i]		=	MathConverter.Convert( Quaternion.RotationMatrix( globalPose[i] ) );
			}

			//	create solver :
			ikSolver	=	new IKSolver();
			joints		=	new List<IKJoint>();
			controls	=	new List<Control>();

			pelvis		=	ExtractBone("pelvis"	, 1);
			spine1		=	ExtractBone("spine1"	, 1);
			spine2		=	ExtractBone("spine2"	, 1);
			chest		=	ExtractBone("chest"		, 1);
			head		=	ExtractBone("head"		, 1);

			L_hip		=	ExtractBone("L_hip"		, 1);
			L_shin		=	ExtractBone("L_shin"	, 1);
			L_foot		=	ExtractBone("L_foot"	, 1);
			L_toe		=	ExtractBone("L_toe"		, 1);

			R_hip		=	ExtractBone("R_hip"		, 1);
			R_shin		=	ExtractBone("R_shin"	, 1);
			R_foot		=	ExtractBone("R_foot"	, 1);
			R_toe		=	ExtractBone("R_toe"		, 1);

			L_shoulder	=	ExtractBone("L_shoulder", 1);
			L_arm		=	ExtractBone("L_arm"		, 1);
			L_hand		=	ExtractBone("L_hand"	, 1);

			R_shoulder	=	ExtractBone("R_shoulder", 1);
			R_arm		=	ExtractBone("R_arm"		, 1);
			R_hand		=	ExtractBone("R_hand"	, 1);

			Attach( joints, pelvis		,	spine1		);
			Attach( joints, spine1		,	spine2		);
			Attach( joints, spine2		,	chest		);
			Attach( joints, chest		,	head		);

			Attach( joints, pelvis		,	L_hip		);
			Attach( joints, L_hip		,	L_shin		);
			Attach( joints, L_shin		,	L_foot		);
			Attach( joints, L_foot		,	L_toe		);

			Attach( joints, pelvis		,	R_hip		);
			Attach( joints, R_hip		,	R_shin		);
			Attach( joints, R_shin		,	R_foot		);
			Attach( joints, R_foot		,	R_toe		);

			Attach( joints, chest		,	L_shoulder	);
			Attach( joints, L_shoulder	,	L_arm		);
			Attach( joints, L_arm		,	L_hand		);

			Attach( joints, chest		,	R_shoulder	);
			Attach( joints, R_shoulder	,	R_arm		);
			Attach( joints, R_arm		,	R_hand		);

			R_handEffector = DragControlBone( controls, R_hand );
			DragControlBone( controls, chest );
			DragControlBone( controls, L_hand );
			DragControlBone( controls, R_foot );
			DragControlBone( controls, L_foot );
			DragControlBone( controls, pelvis );

			bones	=	new[] { 
				pelvis		, 	spine1		,	spine2	, 	chest	,
				head		, 	L_hip		,	L_shin	, 	L_foot	,
				L_toe		, 	R_hip		,	R_shin	,	R_foot	,
				R_toe		,	L_shoulder	,	L_arm	,	L_hand	,
				R_shoulder	,	R_arm		,	R_hand	
			};
		}


		Bone ExtractBone( string name, float mass )
		{
			var index	=	scene.GetNodeIndex( name );
			var p		=	globalTranslation[index];
			var q		=	globalRotation[index];
			var bone	=	new Bone( p, q, 0.5f, 0.5f, mass );
			bone.Index	=	index;

			return bone;
		}


		IKJoint Attach( List<IKJoint> joints, Bone parent, Bone child )
		{
			var joint = new IKBallSocketJoint( parent, child, child.Position );
			joints.Add( joint );
			return joint;
		}


		DragControl DragControlBone( List<Control> controls, Bone bone )
		{
			var ctrl = new DragControl();
			ctrl.TargetBone = bone;
			ctrl.LinearMotor.TargetPosition = bone.Position;

			controls.Add( ctrl );

			return ctrl;
		}

		/*Bone ExtractBone( string name1, string name2, float mass )
		{
			var index	=	scene.GetNodeIndex( name1 );
			var p		=	globalTranslation[index];
			var q		=	globalRotation[index];

			return new Bone( p, q, 0.5f, 0.5f, mass );
		} */


		/// <summary>
		/// 
		/// </summary>
		/// <param name="time"></param>
		/// <param name="destination"></param>
		public bool Evaluate ( GameTime gameTime, DebugRender dr, Matrix worldTransform, Matrix[] destination )
		{
			foreach ( var bone in bones )
			{
				DrawBone( dr, worldTransform, bone );
				destination[ bone.Index ] = GetBoneTransform( bone );
			}

			var p = Vector3.Right * 15 + Vector3.Up * 30 + Vector3.Up * 10 * (float)Math.Cos( gameTime.Total.TotalSeconds );
			R_handEffector.LinearMotor.TargetPosition = MathConverter.Convert( p );
			R_handEffector.LinearMotor.MaximumForce	= 100;
			R_handEffector.LinearMotor.Rigidity	= 0.1f;

			ikSolver.Solve( controls );

			return true;
		}


		Matrix GetBoneTransform( Bone bone )
		{
			var p	= MathConverter.Convert( bone.Position );
			var q	= MathConverter.Convert( bone.Orientation );
			return 	Matrix.RotationQuaternion( q ) * Matrix.Translation( p );
		}


		void DrawBone( DebugRender dr, Matrix worldTransform, Bone bone )
		{
			var m	= Matrix.Scaling(10) * GetBoneTransform(bone) * modelTransform * worldTransform;
			var box	= new BoundingBox( bone.Radius, bone.Height, bone.Radius );

			dr.DrawBasis( m, 0.5f, 1 );
			dr.DrawBox  ( box, m, Color.LightYellow );
		}
	}
}
