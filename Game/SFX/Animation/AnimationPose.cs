using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;

namespace IronStar.SFX {


	public class AnimationPose : AnimationSource {

		public int Frame { get; set; } = 0;

		readonly AnimationTake take;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="blendMode"></param>
		public AnimationPose( Scene scene, string channel, string takeName, AnimationBlendMode blendMode ) : base(scene, channel, blendMode)
		{
			this.take		=	scene.Takes[takeName];

			if (take==null) {
				Log.Warning("Take '{0}' does not exist", takeName );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="time"></param>
		/// <param name="destination"></param>
		public override bool Evaluate ( GameTime gameTime, Matrix[] destination )
		{
			//	apply transofrms :
			if ( Weight==0 ) {
				return false; // bypass track
			}

			bool additive = blendMode==AnimationBlendMode.Additive;

			Frame = MathUtil.Clamp( Frame + take.FirstFrame, take.FirstFrame, take.LastFrame );


			for ( int chIdx = 0; chIdx < channelIndices.Length; chIdx++ ) {
				
				int nodeIndex	= channelIndices[ chIdx ];
				var weight		= Weight;

				Matrix dst		=	destination[nodeIndex];
				Matrix src;

				if (additive) {

					take.GetDeltaKey( Frame, nodeIndex, out src );
					dst = AnimBlendUtils.Lerp( dst, dst * src, weight );

				} else {

					take.GetKey( Frame, nodeIndex, out src );
					dst = AnimBlendUtils.Lerp( dst, src, weight );

				}

				destination[nodeIndex] = dst;
			}

			return true;
		}
	}
}
