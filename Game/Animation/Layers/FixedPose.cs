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

namespace IronStar.Animation 
{
	public class AnimationPose : BaseLayer 
	{
		int frameFrom = 0;
		int frameTo = 0;

		public override bool IsPlaying { get { return true; } }

		public int Frame 
		{ 
			get 
			{
				return (frameFrom==frameTo) ? frameFrom : int.MaxValue;
			}
			set 
			{
				frameFrom = value;
				frameTo   = value;
			}
		}

		public int FrameFrom 
		{ 
			get { return frameFrom; }
			set { frameFrom = value; }
		}

		public int FrameTo 
		{ 
			get { return frameTo; }
			set { frameTo = value; }
		}

		readonly AnimationTake take;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name">Take name. May be null, first track will be set</param>
		/// <param name="blendMode"></param>
		public AnimationPose( Scene scene, string channel, string takeName, AnimationBlendMode blendMode ) : base(scene, channel, blendMode)
		{
			this.take		=	scene.Takes[takeName];

			if (take==null) 
			{
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
			if ( Weight==0 ) 
			{
				return false; // bypass track
			}

			bool additive = blendMode==AnimationBlendMode.Additive;

			Frame = MathUtil.Clamp( Frame + take.FirstFrame, take.FirstFrame, take.LastFrame );


			for ( int chIdx = 0; chIdx < channelIndices.Length; chIdx++ ) 
			{
				int nodeIndex	= channelIndices[ chIdx ];
				var weight		= Weight;

				var dst		=	destination[nodeIndex];
				var src		=	AnimationKey.Identity;

				if (additive) 
				{
					take.GetDeltaKey( Frame, nodeIndex, out src );
					dst = AnimationUtils.Lerp( dst, dst * src.Transform, weight );
				} 
				else 
				{
					take.GetKey( Frame, nodeIndex, ref src );
					dst = AnimationUtils.Lerp( dst, src.Transform, weight );
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
