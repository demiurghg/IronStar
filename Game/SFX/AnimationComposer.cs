using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.Core;

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

		List<FXInstance> fxInstances = new List<FXInstance>();


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
			var jointId	 =	scene.GetNodeIndex( joint );

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

	}
}
