using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics.Scenes;

namespace IronStar.Animation.Layers
{
	public sealed class Pose
	{
		readonly Scene scene;
		readonly AnimationBlendMode blendMode;
		AnimationKey[] targetPose;
		AnimationKey[] blendPose;

		public AnimationKey[] TargetPose { get { return targetPose; }	}
		public AnimationBlendMode BlendMode { get { return blendMode; } }

		TimeSpan		time;
		TimeSpan		transitionTime;
		AnimationCurve	transitionCurve;


		public Pose( Scene scene, AnimationBlendMode blendMode )
		{
			this.scene		=	scene;
			this.blendMode	=	blendMode;
			targetPose		=	new AnimationKey[ scene.Nodes.Count ];
			blendPose		=	new AnimationKey[ scene.Nodes.Count ];

			SetPose(null, 0);
			BlendPose(null, 0, 0);
		}


		/// <summary>
		/// Sets pose
		/// </summary>
		/// <param name="takeName">Name of the take</param>
		/// <param name="frame">Zero based frame index</param>
		/// <returns></returns>
		public void SetPose( AnimationTake take, int frame )
		{
			if (take==null) throw new ArgumentNullException("take");

			frame	=	MathUtil.Clamp( frame + take.FirstFrame, 0, take.FrameCount );

			take.GetPose( frame, blendMode, targetPose );
		}


		/// <summary>
		/// Blends pose on top fo existing pose
		/// </summary>
		/// <param name="takeName"></param>
		/// <param name="frame"></param>
		/// <param name="factor"></param>
		public void BlendPose( AnimationTake take, int frame, float factor )
		{
			if (take==null) throw new ArgumentNullException("take");

			frame	=	MathUtil.Clamp( frame + take.FirstFrame, 0, take.FrameCount );

			take.GetPose( frame, blendMode, blendPose );

			for (int i=0; i<targetPose.Length; i++)
			{
				targetPose[i]	=	AnimationKey.Lerp( targetPose[i], blendPose[i], factor );
			}
		}
	}
}
