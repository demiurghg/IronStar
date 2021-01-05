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
	public abstract class BlendSpace : BaseLayer 
	{
		public override bool IsPlaying { get { return true; } }

		protected readonly AnimationTake take;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name">Take name. May be null, first track will be set</param>
		/// <param name="blendMode"></param>
		public BlendSpace( Scene scene, string channel, string takeName, AnimationBlendMode blendMode ) : base(scene, channel, blendMode)
		{
			this.take		=	scene.Takes[takeName];

			if (take==null) 
			{
				Log.Warning("Take '{0}' does not exist", takeName );
			}
		}


		protected abstract void GetLocalBlendedKey( int nodeIndex, bool additive, ref AnimationKey key );


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

			
			for ( int chIdx = 0; chIdx < channelIndices.Length; chIdx++ ) 
			{
				int nodeIndex	= channelIndices[ chIdx ];
				var weight		= Weight;

				var dst		=	destination[nodeIndex];
				var src		=	AnimationKey.Identity;

				GetLocalBlendedKey( nodeIndex, additive, ref src );

				if (additive) 
				{
					dst = AnimationUtils.Lerp( dst, dst * src.Transform, weight );
				} 
				else 
				{
					dst = AnimationUtils.Lerp( dst, src.Transform, weight );
				}

				destination[nodeIndex] = dst;
			}

			return true;
		}
	}


	public class BlendSpace2 : BlendSpace
	{
		public float Factor { get; set; }
		public int LocalFrame1 { get { return frame1 - take.FirstFrame; } set { frame1 = take.ClampFrameIndex( value + take.FirstFrame ); } }
		public int LocalFrame2 { get { return frame2 - take.FirstFrame; } set { frame2 = take.ClampFrameIndex( value + take.FirstFrame ); } }

		int frame1;
		int frame2;

		public BlendSpace2( Scene scene, string channel, string takeName, AnimationBlendMode blendMode, int frame1, int frame2 )
		:base( scene, channel, takeName, blendMode )
		{
			LocalFrame1	=	frame1;
			LocalFrame2	=	frame2;
		}

		protected override void GetLocalBlendedKey( int nodeIndex, bool additive, ref AnimationKey key )
		{
			var key1 = AnimationKey.Identity;
			var key2 = AnimationKey.Identity;

			if (additive)
			{
				take.GetDeltaKey( frame1, nodeIndex, out key1 );
				take.GetDeltaKey( frame2, nodeIndex, out key2 );
			}
			else
			{
				take.GetKey( frame1, nodeIndex, ref key1 );
				take.GetKey( frame2, nodeIndex, ref key2 );
			}

			key	=	AnimationKey.Lerp( key1, key2, Factor );
		}
	}
	

	public class BlendSpaceD4 : BlendSpace
	{
		public float FactorX { get { return factorX; } set { factorX = MathUtil.Clamp( value, -1f, 1f ); } }
		public float FactorY { get { return factorY; } set { factorY = MathUtil.Clamp( value, -1f, 1f ); } }

		float factorX = 0;
		float factorY = 0;

		//	frame indices
		readonly int f0,f1,f2,f3,f4; 

		public BlendSpaceD4( Scene scene, string channel, string takeName, AnimationBlendMode blendMode )
		:base( scene, channel, takeName, blendMode )
		{
			f0	=	take.ClampFrameIndex( take.FirstFrame + 0 );
			f1	=	take.ClampFrameIndex( take.FirstFrame + 1 );
			f2	=	take.ClampFrameIndex( take.FirstFrame + 2 );
			f3	=	take.ClampFrameIndex( take.FirstFrame + 3 );
			f4	=	take.ClampFrameIndex( take.FirstFrame + 4 );
		}

		protected override void GetLocalBlendedKey( int nodeIndex, bool additive, ref AnimationKey key )
		{
			var keyX = AnimationKey.Identity;
			var keyY = AnimationKey.Identity;
			var key0 = AnimationKey.Identity;

			var factor = MathUtil.Saturate( new Vector2( FactorX, FactorY ).Length() );

			take.GetDeltaKey( FactorX > 0 ? f1 : f2, nodeIndex, out keyX );
			take.GetDeltaKey( FactorY > 0 ? f3 : f4, nodeIndex, out keyY );

			if (additive)	
			{
				take.GetDeltaKey( f0, nodeIndex, out key0 );
			}
			else
			{
				take.GetKey( f0, nodeIndex, ref key0 );
			}

			key0	=	AnimationKey.Lerp( key0, key0 * keyX, Math.Abs( FactorX ) );
			key0	=	AnimationKey.Lerp( key0, key0 * keyY, Math.Abs( FactorY ) );

			key		=	key0;
		}
	}
}
