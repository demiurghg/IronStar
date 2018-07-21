using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.Core;

namespace IronStar.SFX {
	public class AnimationComposer {

		readonly Scene scene;
		readonly AnimationTrackCollection tracks;

		public AnimationTrackCollection Tracks { get { return tracks; } }

		TimeSpan timer;
		int frame;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="scene"></param>
		public AnimationComposer ( Scene scene )
		{
			timer		=	new TimeSpan(0);
			this.scene	=	scene;
			this.tracks	=	new AnimationTrackCollection();
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

			//	copy scene transforms :
			scene.CopyLocalTransformsTo( transforms );

			//	pass transformations through all tracks :
			foreach ( var track in tracks ) {
				track.Evaluate( gameTime, transforms );
			}

			//	increase timer :
			timer = timer + gameTime.Elapsed;
			frame++;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="fxname"></param>
		/// <param name="joint"></param>
		public void SequenceFX ( string fxname, string joint )
		{
			throw new NotImplementedException();
		}

	}
}
