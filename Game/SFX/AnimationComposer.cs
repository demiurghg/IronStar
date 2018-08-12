using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Engine.Audio;
using IronStar.Core;
using Fusion.Core.Extensions;

namespace IronStar.SFX {
	public class AnimationComposer {

		readonly Scene scene;
		readonly string name;
		readonly ModelInstance model;
		readonly AnimationStack tracks;

		public AnimationStack Tracks { get { return tracks; } }

		TimeSpan timer;
		int frame;

		readonly FXPlayback fxPlayback;
		readonly GameWorld world;
		readonly Matrix[] localTransforms;

		List<FXInstance> fxInstances = new List<FXInstance>(32);
		List<SoundEventInstance> soundInstances = new List<SoundEventInstance>(32);


		/// <summary>
		/// 
		/// </summary>
		/// <param name="scene"></param>
		public AnimationComposer ( string name, ModelInstance model, Scene scene, GameWorld world )
		{
			timer			=	new TimeSpan(0);
			this.name		=	name;
			this.model		=	model;
			this.scene		=	scene;
			this.tracks		=	new AnimationStack();
			this.fxPlayback	=	world.FXPlayback;
			this.world		=	world;

			localTransforms	=	new Matrix[scene.Nodes.Count];
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="gameTime"></param>
		public void Update ( GameTime gameTime, Matrix[] transforms )
		{
			if (transforms==null) {
				throw new ArgumentNullException("transforms");
			}
			if (transforms.Length!=scene.Nodes.Count) {
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
				Log.Warning("{0} : no animation applied!", name);
			}

			//--------------------------------
			//	compute global transforms 
			//	required for FX and IK :
			scene.ComputeAbsoluteTransforms( localTransforms, transforms );

			//--------------------------------
			//	update FX :
			foreach ( var fxInstance in fxInstances ) {
				Vector3 p, s;
				Quaternion q;
				Matrix jointWorld = transforms[ fxInstance.JointIndex ] * model.PreTransform * model.ComputeWorldMatrix();
				jointWorld.Decompose( out s, out q, out p );
				fxInstance.Move( p, Vector3.Zero, q );
			}

			fxInstances.RemoveAll( fx => fx.IsExhausted );

			//--------------------------------
			//	update sound :
			foreach ( var sound in soundInstances ) {
				Vector3 position = 	model.ComputeWorldMatrix().TranslationVector;
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
			int jointId = 0;

			if (joint==null) {
				jointId	 =	0;
			} else {
				jointId	 =	scene.GetNodeIndex( joint );
			}

			if (jointId<0) {
				Log.Warning("Bad joint name: {0}", joint);
			}

			var fxAtom	 = world.Atoms[ fxName ];
			var fxEvent  = new FXEvent( fxAtom, 0, Vector3.Zero, Vector3.Zero, Quaternion.Identity );
				fxEvent.Scale = scale;
			var instance = fxPlayback.RunFX( fxEvent, false );

			instance.JointIndex = jointId;
			instance.WeaponFX = model.IsFPVModel;

			fxInstances.Add( instance );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxname"></param>
		/// <param name="joint"></param>
		public void SequenceSound ( string soundEventName )
		{
			try {

				var ss		=	world.Game.GetService<SoundSystem>();

				if (string.IsNullOrWhiteSpace(soundEventName)) {
					return;
				}
				
				var soundEvent		=	ss.GetEvent( soundEventName );
				var soundInstance	=	soundEvent.CreateInstance();
				
				soundInstance.Set3DParameters( model.ComputeWorldMatrix().TranslationVector );
				soundInstance.ReverbLevel = 1;
				soundInstance.Start();
				
				soundInstances.Add( soundInstance );

			} catch ( SoundException e ) {
				Log.Warning( e.Message );
			}
		}

	}
}
