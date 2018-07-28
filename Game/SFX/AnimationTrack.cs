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


	public class AnimationTrack {

		public float TimeScale { get; set; } = 1;
		public float Weight { get; set; } = 1;
		public AnimationBlendMode BlendMode { get; set; }

		public bool Busy { 
			get {
				return currentAnim!=null;
			}
		}

		public int Frame {
			get {
				return frame;
			}
			set {
				frame = value;
			}
		}
		
		readonly string channel;
		readonly Scene scene;
		readonly int nodeCount;
		readonly int[] channelIndices;
		readonly int channelIndex;

		int frame;

		Animation currentAnim;
		Animation pendingAnim;

		class Animation {
			public readonly AnimationTake Take;
			public readonly bool Looped;

			public Animation ( AnimationTake take, bool looped ) 
			{
				this.Take	=	take;
				this.Looped	=	looped;
			}

			public void GetKey ( int node, int frame, out Matrix transform )
			{
				Take.GetKey( frame + Take.FirstFrame, node, out transform );
			}

			public void GetDeltaKey ( int node, int frame, out Matrix transform )
			{
				Take.GetDeltaKey( frame + Take.FirstFrame, node, out transform );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="blendMode"></param>
		public AnimationTrack( Scene scene, string channel, AnimationBlendMode blendMode )
		{
			this.channel	=	channel;
			BlendMode		=	blendMode;
			this.scene		=	scene;
			nodeCount		=	scene.Nodes.Count;

			channelIndex	=	scene.GetNodeIndex( channel );

			if (channelIndex<0) {
				Log.Warning("Channel joint '{0}' does not exist", channel );
			}

			channelIndices	=	scene.GetChannelNodeIndices( channelIndex );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="time"></param>
		/// <param name="destination"></param>
		public bool Evaluate ( GameTime gameTime, Matrix[] destination )
		{
			//	update sequence twice 
			//	to avoid stucking 
			//	on extremely short clips.
			UpdateSequence();
			UpdateSequence();

			//	apply transofrms :
			bool r = ApplyTransforms( frame, destination );

			//	advance time :
			frame += (int)(1 * TimeScale);

			return r;
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="clip"></param>
		/// <param name="immediate"></param>
		/// <param name="looped"></param>
		/// <param name="crossfade"></param>
		public void Sequence ( string takeName, bool immediate, bool looped )
		{
			var take = scene.Takes[ takeName ];

			Log.Verbose(" seq : {0} {1} {2}", takeName, immediate ? "immediate":"", looped ? "looped":"" );

			if (take==null) {
				Log.Warning("Take '{0}' does not exist", takeName );
				return;
			}


			var anim = new Animation ( take, looped );

			if (currentAnim==null || immediate) {

				currentAnim		=	anim;
				pendingAnim		=	null;
				frame			=	0;

			} else {
				pendingAnim		=	anim;
			}
		}



		/*-----------------------------------------------------------------------------------------
		 *  Internal stuff :
		-----------------------------------------------------------------------------------------*/

		bool ApplyTransforms ( int frame, Matrix[] destination )
		{
			if ( currentAnim==null || Weight==0 ) {
				return false; // bypass track
			}

			bool additive = BlendMode==AnimationBlendMode.Additive;

			for ( int chIdx = 0; chIdx < channelIndices.Length; chIdx++ ) {
				
				int nodeIndex	= channelIndices[ chIdx ];
				var weight		= Weight;

				Matrix dst		=	destination[nodeIndex];
				Matrix src;

				if (additive) {

					currentAnim.GetDeltaKey( nodeIndex, frame, out src );
					dst = AnimBlendUtils.Lerp( dst, dst * src, weight );

				} else {

					currentAnim.GetKey( nodeIndex, frame, out src );
					dst = AnimBlendUtils.Lerp( dst, src, weight );

				}

				destination[nodeIndex] = dst;
			}

			return true;
		}



		/// <summary>
		/// Update sequenced animations
		/// </summary>
		void UpdateSequence ()
		{
			if (currentAnim!=null) {

				int frameCount = currentAnim.Take.FrameCount;
			
				if ( frame >= frameCount ) {

					if ( pendingAnim!=null ) {

						frame %= frameCount;

						currentAnim = pendingAnim;
						pendingAnim = null;

					} else {

						if ( currentAnim.Looped ) {
							frame %= frameCount;
						} else {
							currentAnim = null;
						}
					}
				}
			}
		}
	}
}
