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
	public class GaitLayer : BaseLayer 
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

		float	travalledDistance = 0;


		Matrix[]	stance;
		Matrix[]	rightLegUp;
		Matrix[]	rightLegBack;
		Matrix[]	leftLegUp;
		Matrix[]	leftLegBack;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name">Take name. May be null, first track will be set</param>
		/// <param name="blendMode"></param>
		public GaitLayer( Scene scene, string channel, string takeName, AnimationBlendMode blendMode ) : base(scene, channel, blendMode)
		{
			this.take			=	scene.Takes[takeName];
			travalledDistance	=	0;

			if (take==null) 
			{
				Log.Warning("Take '{0}' does not exist", takeName );
			}

			stance			=	take.GetPose( 2 );

			rightLegUp		=	take.GetPose( 3 );
			rightLegBack	=	take.GetPose( 4 );
			leftLegUp		=	take.GetPose( 5 );
			leftLegBack		=	take.GetPose( 6 );
		}


		public void Advance( float groundVelocity, GameTime gameTime )
		{
			travalledDistance += groundVelocity * gameTime.ElapsedSec;
		}


		int GetWalkFrameIndex( int index )
		{
			return 3 + (index % 4);
		}

		int GetRunFrameIndex( int index )
		{
			return 7 + (index % 4);
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

			Frame = MathUtil.Clamp( Frame + take.FirstFrame, take.FirstFrame, take.LastFrame );


			float frame		=	travalledDistance / 1.7f * 0.32f * 2;
			float factor	=	frame % 1.0f;
			int frame0		=	(int)Math.Floor( frame );
			int frame1		=	frame0 + 1;

			int walkFrame0	=	GetWalkFrameIndex( frame0 );
			int walkFrame1	=	GetWalkFrameIndex( frame1 );
			int runFrame0	=	GetRunFrameIndex( frame0 );
			int runFrame1	=	GetRunFrameIndex( frame1 );

			for ( int chIdx = 0; chIdx < channelIndices.Length; chIdx++ ) 
			{
				int nodeIndex	= channelIndices[ chIdx ];
				var weight		= Weight;

				Matrix key0		=	take.GetKey( runFrame0, nodeIndex );
				Matrix key1		=	take.GetKey( runFrame1, nodeIndex );

				Matrix key		=	AnimationUtils.Lerp( key0, key1, factor );

				destination[nodeIndex] = key;
			}

			return true;
		}
	}
}
