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
using IronStar.Core;
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

		List<FXInstance> fxInstances = new List<FXInstance>(32);
		List<SoundEventInstance> soundInstances = new List<SoundEventInstance>(32);


		/// <summary>
		/// 
		/// </summary>
		/// <param name="scene"></param>
		public AnimationComposer ( FXPlayback fxPlayback, RenderModelInstance model, Scene scene )
		{
			timer			=	new TimeSpan(0);
			this.model		=	model;
			this.scene		=	scene;
			this.tracks		=	new AnimationStack();
			this.fxPlayback	=	fxPlayback;

			localTransforms	=	new Matrix[scene.Nodes.Count];
		}


		/// <summary>
		/// Updates entire composer states and all underlaying tracks, sounds and particle effects.
		/// Generates absolute/flat model-space transformations :
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="transforms"></param>
		public void Update ( GameTime gameTime, Matrix[] flatTransforms )
		{
			if (flatTransforms==null) {
				throw new ArgumentNullException("transforms");
			}
			if (flatTransforms.Length!=scene.Nodes.Count) {
				throw new ArgumentOutOfRangeException("transforms.Length != scene.Nodes.Count");
			}

			//--------------------------------
			//	copy scene transforms :
			scene.CopyLocalTransformsTo( localTransforms );
			#warning support animation bypass!

			//--------------------------------
			//	pass transformations through all tracks :
			bool anim = false;

			foreach ( var track in tracks ) {
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
			foreach ( var fxInstance in fxInstances ) {
				Vector3 p, s;
				Quaternion q;
				Matrix jointWorld = flatTransforms[ fxInstance.JointIndex ] * model.ModelFeatureWorldMatrix;
				jointWorld.Decompose( out s, out q, out p );
				fxInstance.Move( p, Vector3.Zero, q );
			}

			fxInstances.RemoveAll( fx => fx.IsExhausted );

			//--------------------------------
			//	update sound :
			foreach ( var sound in soundInstances ) {
				Vector3 position = 	model.ModelFeatureWorldMatrix.TranslationVector;
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
				fxEvent.Scale	=	scale;
			
			var instance = fxPlayback.RunFX( fxName, fxEvent, false, false /* -- this is not ECS FX! */ );

			if (instance!=null)
			{
				instance.JointIndex	=	jointId;
				instance.WeaponFX	=	model.IsFPVModel;
				fxInstances.Add( instance );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxname"></param>
		/// <param name="joint"></param>
		public void SequenceSound ( string soundEventName )
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
				
				soundInstance.Set3DParameters( model.ModelFeatureWorldMatrix.TranslationVector );
				soundInstance.ReverbLevel = 1;
				soundInstance.Start();
				
				soundInstances.Add( soundInstance );
			} 
			catch ( SoundException e ) 
			{
				Log.Warning( e.Message );
			}
		}


		public TakeSequencer GetAdditiveIdleSequencer()
		{
			foreach ( var track in tracks )
			{
				if ( track is TakeSequencer )
				{
					var sequencer = (TakeSequencer)track;
					
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
