using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;

namespace IronStar.SFX {
	public class AnimationClip {

		readonly Scene scene;
		readonly AnimationTake take;
		readonly Matrix[] refPose;

		public AnimationTake Take { get { return take; } }


		/// <summary>
		/// 
		/// </summary>
		/// <param name="scene"></param>
		/// <param name="takeName"></param>
		public AnimationClip ( Scene scene, string takeName )
		{
			this.scene	=	scene;
			take		=	scene.Takes.FirstOrDefault( t => t.Name == takeName );

			if (take==null) {
				throw new ArgumentException("Take '{0}' does not exist", takeName);
			}

			//	compute base pose for additive animations :
			refPose	=	new Matrix[ scene.Nodes.Count ];

			take.Evaluate( take.FirstFrame, AnimationWrapMode.Clamp, refPose );

			for ( int i=0; i<refPose.Length; i++ ) {
				refPose[i] = Matrix.Invert( refPose[i] );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="frame"></param>
		/// <param name="additive"></param>
		/// <returns></returns>
		public Matrix Sample ( Matrix transform, int node, int frame, bool additive, float weight )
		{
			Matrix sample;
			take.GetKey( frame, node, out sample );

			if (additive) {

				sample	=	sample * refPose[node];
				return	AnimBlendUtils.Lerp( transform, transform * sample, weight );

			} else {

				return AnimBlendUtils.Lerp( transform, sample, weight );

			}

		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="time"></param>
		public void Evaluate ( int frame, Matrix[] destination )
		{
			take.Evaluate( frame - take.FirstFrame, AnimationWrapMode.Repeat, destination );
		}


	}
}
