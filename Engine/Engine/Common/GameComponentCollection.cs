using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace Fusion.Engine.Common {

	#error Concurrency?
    public sealed class GameComponentCollection : Collection<IGameComponent> {

        public event EventHandler<GameComponentCollectionEventArgs> ComponentAdded;
        public event EventHandler<GameComponentCollectionEventArgs> ComponentRemoved;

		public class GameComponentCollectionEventArgs : EventArgs {
			public readonly IGameComponent GameComponent;

			public GameComponentCollectionEventArgs(IGameComponent gameComponent)
			{
				this.GameComponent = gameComponent;
			}
		}
		

        protected override void ClearItems()
        {
            foreach ( var item in this ) {
				ComponentRemoved?.Invoke(this, new GameComponentCollectionEventArgs(item));                
            }
            base.ClearItems();
        }


        protected override void InsertItem(int index, IGameComponent item)
        {
            if (item == null) {
                throw new ArgumentNullException("Null component");
            }
            if (base.IndexOf(item) != -1) {
                throw new ArgumentException("Cannot add same component multiple times");
            }
            base.InsertItem(index, item);

			ComponentAdded?.Invoke( this, new GameComponentCollectionEventArgs(item) );
        }


        protected override void RemoveItem(int index)
        {
			IGameComponent gameComponent = base[index];
			base.RemoveItem(index);
			ComponentRemoved?.Invoke(this, new GameComponentCollectionEventArgs(gameComponent));
        }


        protected override void SetItem(int index, IGameComponent item)
        {
            throw new NotSupportedException();
        }
    }
}
