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

namespace IronStar.Animation.IK
{
	public class FBIKAnimator
	{
		readonly Scene	scene;
		readonly Matrix transform;

		public Effector Head		= new Effector();
		public Effector Chest		= new Effector();
		public Effector Pelvis		= new Effector();
		public Effector LeftHand	= new Effector();
		public Effector RightHand	= new Effector();
		public Effector LeftFoot	= new Effector();
		public Effector RightFoot	= new Effector();
		public Effector LeftToe		= new Effector();
		public Effector RightToe	= new Effector();

		Matrix[] bindPose;


		/// <summary>
		/// 
		/// </summary>
		public FBIKAnimator( Scene scene, Matrix modelTransform )
		{
			this.transform	=	modelTransform;
			this.scene		=	scene;

			bindPose	=	new Matrix[ scene.Nodes.Count ];
			scene.CopyLocalTransformsTo( bindPose );

			MapSkeletonToAffectors();
		}


		void MapSkeletonToAffectors()
		{
			var globalPose = new Matrix[ scene.Nodes.Count ];
			scene.ComputeAbsoluteTransforms( globalPose );

			Head		=	GetEffector( globalPose, IKSkeleton.HEAD	);
			Chest		=	GetEffector( globalPose, IKSkeleton.CHEST	);
			Pelvis		=	GetEffector( globalPose, IKSkeleton.PELVIS	);
			LeftHand	=	GetEffector( globalPose, IKSkeleton.L_HAND	);
			RightHand	=	GetEffector( globalPose, IKSkeleton.R_HAND	);
			LeftFoot	=	GetEffector( globalPose, IKSkeleton.L_FOOT	);
			RightFoot	=	GetEffector( globalPose, IKSkeleton.R_FOOT	);
			LeftToe		=	GetEffector( globalPose, IKSkeleton.L_TOE	);
			RightToe	=	GetEffector( globalPose, IKSkeleton.R_TOE	);
		}


		Effector GetEffector( Matrix[] globalPose, string boneName )
		{
			var index = scene.GetNodeIndex( boneName );
			
			if (index<0) 
			{
				return null;
			}

			var effector = new Effector();
			effector.Position	=	globalPose[index].TranslationVector;
			effector.Rotation	=	Quaternion.Identity;

			return effector;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="time"></param>
		/// <param name="destination"></param>
		public bool Evaluate ( GameTime gameTime, DebugRender dr, Matrix worldTransform, Matrix[] destination )
		{
			for ( int index=0; index<scene.Nodes.Count; index++ ) 
			{
				Matrix key			=	bindPose[index];
				destination[index]	=	key;
			}


			scene.ComputeAbsoluteTransforms( bindPose, destination );

			DrawEffector( dr, worldTransform, Head		);
			DrawEffector( dr, worldTransform, Chest		);
			DrawEffector( dr, worldTransform, Pelvis	);	
			DrawEffector( dr, worldTransform, LeftHand	);
			DrawEffector( dr, worldTransform, RightHand	);
			DrawEffector( dr, worldTransform, LeftFoot	);
			DrawEffector( dr, worldTransform, RightFoot	);
			DrawEffector( dr, worldTransform, LeftToe	);	
			DrawEffector( dr, worldTransform, RightToe	);

			return true;
		}



		void DrawEffector( DebugRender dr, Matrix worldMatrix, Effector effector )
		{	
			var position = Vector3.TransformCoordinate( effector.Position, transform * worldMatrix );
			dr.DrawPoint( position, 0.5f, Color.Red );
		}
	}
}
