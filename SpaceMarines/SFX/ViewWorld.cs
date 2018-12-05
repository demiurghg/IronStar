using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;

namespace SpaceMarines.SFX {
	public class ViewWorld : GameComponent {

		RenderSystem rs;

		LayerCollection layers;
		public LayerCollection Layers { get { return layers; } }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public ViewWorld( Game game ) : base( game )
		{
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			this.rs			=	Game.GetService<RenderSystem>();

			layers			=	new LayerCollection(rs);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			SafeDispose( ref layers );

			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update( GameTime gameTime )
		{
			base.Update( gameTime );
		}

	}
}
