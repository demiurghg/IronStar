using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;

namespace Fusion.Engine.Campaign {


	public partial class SinglePlayer : GameComponent {

		abstract class GameState {

			public abstract void Start( string map );
			public abstract void Stop();
			public abstract void Pause();
			
		}

	}
}
