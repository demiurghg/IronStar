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
using BEPUphysics;
using BEPUphysics.Character;


namespace IronStar.Views {

	/// <summary>
	/// Class that responce for all first-person view stuff, like weapons etc.
	/// </summary>
	public class FPView : GameComponent {

		readonly GameWorld	world;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="space"></param>
		public FPView ( GameWorld world ) : base( world.Game )
		{
			this.world	=	world;
		}


		public override void Initialize()
		{
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing) {
			}
			base.Dispose( disposing );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update ( float elapsedTime, float lerpFactor )
		{
			var rw	= Game.RenderSystem.RenderWorld;
			var vp	= Game.RenderSystem.DisplayBounds;
		}
	}
}
