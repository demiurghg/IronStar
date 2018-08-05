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

	/// <summary>
	/// Class to control composer depending on entity and model state
	/// </summary>
	public abstract class Animator {

		protected readonly Entity entity;
		protected readonly ModelInstance model;
		protected readonly AnimationComposer composer;

		/// <summary>
		/// 
		/// </summary>
		public Animator ( GameWorld world, Entity entity, ModelInstance model )
		{
			this.entity	=	entity;
			this.model	=	model;
			composer	=	new AnimationComposer( "FPV weapon", model, model.Scene, world );
		}


		/// <summary>
		/// 
		/// </summary>
		public abstract void Update ( GameTime gameTime, Matrix[] destination );

	}
}
