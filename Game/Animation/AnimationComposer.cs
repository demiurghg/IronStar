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
using Fusion.Engine.Audio;
using Fusion.Core.Extensions;
using KopiLua;
using Fusion.Scripting;
using IronStar.SFX;
using IronStar.SFX2;

namespace IronStar.Animation 
{
	public class AnimationComposer 
	{
		readonly Scene scene;
		readonly AnimationStack tracks;

		public AnimationStack Tracks { get { return tracks; } }

		TimeSpan timer;
		int frame;

		readonly FXPlayback fxPlayback;
		readonly Matrix[] localTransforms;
		readonly RenderModelInstance model;
		readonly Queue<string> soundQueue;
		readonly Queue<FXEvent> fxQueue;

		List<FXInstance> fxInstances = new List<FXInstance>(32);
		List<SoundEventInstance> soundInstances = new List<SoundEventInstance>(32);


		/// <summary>
		/// 
		/// </summary>
		/// <param name="scene"></param>
		public AnimationComposer ( FXPlayback fxPlayback, Scene scene )
		{
			timer			=	new TimeSpan(0);
			this.scene		=	scene;
			this.tracks		=	new AnimationStack();
			this.fxPlayback	=	fxPlayback;
			this.soundQueue	=	new Queue<string>();
			this.fxQueue	=	new Queue<FXEvent>();

			localTransforms	=	new Matrix[scene.Nodes.Count];
		}


		/// <summary>
		/// Updates entire composer states and all underlaying tracks, sounds and particle effects.
		/// Generates absolute/flat model-space transformations :
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="transforms"></param>
		public void Update ( GameTime gameTime, Matrix worldTransform, bool isFpv, Matrix[] flatTransforms )
		{
			if (flatTransforms==null) {
				throw new ArgumentNullException("transforms");
			}
			if (flatTransforms.Length<scene.Nodes.Count) {
				throw new ArgumentOutOfRangeException("transforms.Length < scene.Nodes.Count");
			}

			//--------------------------------
			//	copy scene transforms :
			scene.CopyLocalTransformsTo( localTransforms );
			#warning support animation bypass!

			//--------------------------------
			//	pass transformations through all tracks :
			bool anim = false;

			foreach ( var track in tracks ) 
			{
				anim |= track.Evaluate( gameTime, localTransforms );
			}

			if (!anim) {
				Log.Warning("Animation composer: no animation applied!");
			}

			//--------------------------------
			//	compute global transforms 
			//	required for FX and IK :
			scene.ComputeAbsoluteTransforms( localTransforms, flatTransforms );

			//--------------------------------
			//	update FX :
			while ( fxQueue.Any() )
			{
				SequenceFX( fxQueue.Dequeue(), worldTransform, isFpv, flatTransforms );
			}

			foreach ( var fxInstance in fxInstances ) 
			{
				Vector3 p, s;
				Quaternion q;
				Matrix jointWorld = flatTransforms[ fxInstance.JointIndex ] * worldTransform;
				jointWorld.Decompose( out s, out q, out p );
				fxInstance.Move( p, Vector3.Zero, q );
			}

			fxInstances.RemoveAll( fx => fx.IsExhausted );

			//--------------------------------
			//	update sound :
			while ( soundQueue.Any() )
			{
				SequenceSound( soundQueue.Dequeue(), worldTransform );
			}

			foreach ( var sound in soundInstances ) 
			{
				Vector3 position = 	worldTransform.TranslationVector;
				sound.Set3DParameters( position );
			}

			soundInstances.RemoveAll( snd => snd.IsStopped );

			//--------------------------------
			//	increase timer :
			timer = timer + gameTime.Elapsed;
			frame++;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxname"></param>
		/// <param name="joint"></param>
		public void SequenceFX ( string fxName, string joint, float scale )
		{
			int jointId = joint==null ? 0 : scene.GetNodeIndex( joint );

			if (jointId<0) 
			{
				Log.Warning("SequenceFX: Bad joint name: {0}", joint);
				return;
			}

			var fxEvent			=	new FXEvent();
				fxEvent.FXName	=	fxName;
				fxEvent.Scale	=	scale;
				fxEvent.JointId	=	jointId;

			fxQueue.Enqueue( fxEvent );
			
		}


		void SequenceFX( FXEvent fxEvent, Matrix worldTransform, bool isFpv, Matrix[] transforms )
		{
			var instance = fxPlayback?.RunFX( fxEvent, false, false /* -- this is not ECS FX! */ );

			if (instance!=null)
			{
				instance.JointIndex	=	fxEvent.JointId;
				instance.WeaponFX	=	isFpv;
				fxInstances.Add( instance );
			}
		}


		public void SequenceSound( string soundEventName )
		{
			soundQueue.Enqueue( soundEventName );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxname"></param>
		/// <param name="joint"></param>
		void SequenceSound ( string soundEventName, Matrix worldMatrix )
		{
			try 
			{
				var ss	=	fxPlayback.Game.GetService<SoundSystem>();

				if (string.IsNullOrWhiteSpace(soundEventName)) 
				{
					return;
				}
				
				var soundEvent		=	ss.GetEvent( soundEventName );
				var soundInstance	=	soundEvent.CreateInstance();
				
				soundInstance.Set3DParameters( worldMatrix.TranslationVector );
				soundInstance.ReverbLevel = 1;
				soundInstance.Start();
				
				soundInstances.Add( soundInstance );
			} 
			catch ( SoundException e ) 
			{
				Log.Warning( e.Message );
			}
		}


		public Sequencer GetAdditiveIdleSequencer()
		{
			foreach ( var track in tracks )
			{
				if ( track is Sequencer )
				{
					var sequencer = (Sequencer)track;
					
					if (!sequencer.IsPlaying && sequencer.blendMode==AnimationBlendMode.Additive)
					{
						return sequencer;
					}
				}
			}

			return null;
		}
	}
}
