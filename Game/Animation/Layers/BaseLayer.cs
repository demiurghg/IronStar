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

namespace IronStar.Animation 
{
	public abstract class BaseLayer : ITransformProvider
	{
		public abstract bool IsPlaying { get; }
		public float Weight { get; set; } = 1;

		readonly public AnimationBlendMode blendMode;
		
		readonly protected string channel;
		readonly protected Scene scene;
		readonly protected int nodeCount;
		readonly protected int[] channelIndices;
		readonly protected int channelIndex;

		public Scene Scene { get { return scene; } }


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="blendMode"></param>
		public BaseLayer( Scene scene, string channel, AnimationBlendMode blendMode )
		{
			this.channel	=	channel;
			this.blendMode	=	blendMode;
			this.scene		=	scene;
			nodeCount		=	scene.Nodes.Count;

			if (string.IsNullOrEmpty(channel))
			{
				channelIndex	=	0;
				channelIndices	=	scene.GetChannelNodeIndices(-1);
			}
			else
			{
				channelIndex	=	scene.GetNodeIndex( channel );

				if (channelIndex<0) {
					Log.Warning("Channel joint '{0}' does not exist", channel );
				}

				channelIndices	=	scene.GetChannelNodeIndices( channelIndex );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="time"></param>
		/// <param name="destination"></param>
		public abstract bool Evaluate ( GameTime gameTime, Matrix[] destination );
	}
}
