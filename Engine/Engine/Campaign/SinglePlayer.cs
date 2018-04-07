using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;

namespace Fusion.Engine.Campaign {


	public partial class SinglePlayer : GameComponent {


		/// <summary>
		/// Creates SinglePlayer component
		/// </summary>
		/// <param name="game"></param>
		public SinglePlayer ( Game game ) : base(game)
		{
		}


		/// <summary>
		/// Initializes SinglePlayer
		/// </summary>
		public override void Initialize()
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
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
		public void Update ( GameTime gameTime )
		{
		}
	}
}
