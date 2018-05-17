using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace Fusion.Core {

	public class GameComponentCollectionEventArgs : EventArgs {
		public readonly IGameComponent GameComponent;

		public GameComponentCollectionEventArgs(IGameComponent gameComponent)
		{
			this.GameComponent = gameComponent;
		}
	}
}
