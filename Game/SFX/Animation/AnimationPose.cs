using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Scripting;
using KopiLua;

namespace IronStar.SFX {


	public class AnimationPose : AnimationSource {

		int frameFrom = 0;
		int frameTo = 0;


		public int Frame { 
			get {
				return (frameFrom==frameTo) ? frameFrom : int.MaxValue;
			}
			set {
				frameFrom = value;
				frameTo   = value;
			}
		}

		public int FrameFrom { 
			get { return frameFrom; }
			set { frameFrom = value; }
		}

		public int FrameTo { 
			get { return frameTo; }
			set { frameTo = value; }
		}

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

		/*-----------------------------------------------------------------------------------------
		 * 
		 *  Lua API :
		 *  
		-----------------------------------------------------------------------------------------*/

		[LuaApi("set_frame")]
		int SetFrame ( LuaState L )
		{
			Frame = Lua.LuaToInteger( L, 1 );
			return 0;
		}


		[LuaApi("set_weight")]
		int SetWeight ( LuaState L )
		{
			Weight = (float)Lua.LuaToNumber( L, 1 );
			return 0;
		}
	}
}
