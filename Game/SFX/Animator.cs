using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion.Engine.Audio;
using IronStar.Views;


namespace IronStar.SFX {

	public sealed partial class Animator {

		readonly ModelInstance modelInstance;

		readonly int[] channelsAll		;
		readonly int[] channelsTorso	;
		readonly int[] channelsLegs		;
		readonly int[] channelsHead		;
		readonly int[] channelsWeapon	;

		readonly Matrix[] transforms0;
		readonly Matrix[] transforms1;

		readonly int[][] channels;

		readonly List<AnimEvent> animEvents = new List<AnimEvent>();
		readonly List<AnimLoop>	 animLoops  = new List<AnimLoop>();

	
		/// <summary>
		/// 
		/// </summary>
		/// <param name="scene"></param>
		/// <param name="channels"></param>
		public Animator ( ModelInstance modelInstance )
		{
			this.modelInstance	=	modelInstance;

			var scene	=	modelInstance.Scene;

			//
			//	channel stuff :
			//	
			channelsAll		=	scene.GetChannelNodeIndices( 0 );
			channelsTorso	=	scene.GetChannelNodeIndices( scene.GetNodeIndex("torso"	) );
			channelsLegs	=	scene.GetChannelNodeIndices( scene.GetNodeIndex("legs"	) );
			channelsHead	=	scene.GetChannelNodeIndices( scene.GetNodeIndex("head"	) );
			channelsWeapon	=	scene.GetChannelNodeIndices( scene.GetNodeIndex("weapon") );

			channels		=	new int[(int)AnimChannel.Max][];

			channels[(int)AnimChannel.All	]	=	channelsAll		;
			channels[(int)AnimChannel.Torso	]	=	channelsTorso	;
			channels[(int)AnimChannel.Legs	]	=	channelsLegs	;
			channels[(int)AnimChannel.Head	]	=	channelsHead	;
			channels[(int)AnimChannel.Weapon]	=	channelsWeapon	;

			//
			//	transform :
			//	
			transforms0		=	new Matrix[ scene.Nodes.Count ];
			transforms1		=	new Matrix[ scene.Nodes.Count ];
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="animChannel"></param>
		/// <returns></returns>
		int[] GetChannelIndices ( AnimChannel animChannel )
		{
			return channels[ (int)animChannel ];
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="dt"></param>
		/// <param name="transforms"></param>
		public void Update ( float elapsedTime, Matrix[] outputTransforms )
		{
			//	compute default transforms :
			modelInstance.Scene.CopyLocalTransformsTo( outputTransforms );

			//	sort animation events :
			var animEventsSorted = animEvents.OrderBy( ae => ae.Channel );

			//	... than play and apply them :
			foreach ( var ae in animEventsSorted ) {
				ae.UpdateAndBlend( elapsedTime, outputTransforms );
			}

			//	remove completed :
			animEvents.RemoveAll( ae => ae.IsCompleted );

			//	compute global transforms :
			modelInstance.Scene.ComputeAbsoluteTransforms( outputTransforms, outputTransforms );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="clipName"></param>
		public void PlayEvent ( AnimChannel channel, string clipName, float weight, float fadein, float fadeout )
		{
			var clip = modelInstance.Clips.FirstOrDefault( c => c.TakeName == clipName );

			if (clip==null) {
				Log.Warning("Animator: clip {0} does not exist", clipName );
			}

			var channelIndices = channels[(int)channel];

			if (channelIndices.Length==0) {
				Log.Warning("Animator: channel {0} is empty", channel );
			}

			var animEvent = new AnimEvent( this, channel, clip, weight, fadein, fadeout );

			animEvents.Add( animEvent );
		}
	}
}
