using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics {

	internal class RenderComponent : DisposableBase, IGameComponent {

		protected readonly Game Game;
		protected readonly RenderSystem rs;
		protected readonly GraphicsDevice device;

		/// <summary>
		/// Creates instance of render component 
		/// </summary>
		public RenderComponent ( RenderSystem rs )
		{
			this.rs		=	rs;
			this.Game	=	rs.Game;
			this.device	=	rs.Game.GraphicsDevice;
		}


		/// <summary>
		/// Initializes render component
		/// </summary>
		public virtual void Initialize ()
		{
		}


		/// <summary>
		/// Disposes render component stuff...
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
			}

			base.Dispose( disposing );
		}
	}
}
