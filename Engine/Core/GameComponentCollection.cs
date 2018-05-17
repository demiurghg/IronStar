using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace Fusion.Core {

	/// <summary>
	/// Game component collection.
	/// This collection is assumed to be thread safe...
	/// </summary>
    public sealed class GameComponentCollection : Collection<IGameComponent> {

		private readonly object lockObj = new object();

        public event EventHandler<GameComponentCollectionEventArgs> ComponentAdded;
        public event EventHandler<GameComponentCollectionEventArgs> ComponentRemoved;

		public class GameComponentCollectionEventArgs : EventArgs {
			public readonly IGameComponent GameComponent;

			public GameComponentCollectionEventArgs(IGameComponent gameComponent)
			{
				this.GameComponent = gameComponent;
			}
		}
		

		/// <summary>
		/// 
		/// </summary>
        protected override void ClearItems()
        {
			lock (lockObj) {
				foreach ( var item in this ) {
					ComponentRemoved?.Invoke(this, new GameComponentCollectionEventArgs(item));                
				}
				base.ClearItems();
			}
        }


		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="item"></param>
        protected override void InsertItem(int index, IGameComponent item)
        {
			lock (lockObj) {
				if (item == null) {
					throw new ArgumentNullException("Null component");
				}
				if (base.IndexOf(item) != -1) {
					throw new ArgumentException("Cannot add same component multiple times");
				}
				base.InsertItem(index, item);

				ComponentAdded?.Invoke( this, new GameComponentCollectionEventArgs(item) );
			}
        }


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		internal IGameComponent[] ToArrayThreadSafe ()
		{
			lock (lockObj) {
				return this.ToArray();
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public void DisposeAndClear ()
		{
			lock (lockObj) {
				foreach ( var component in this ) {
					(component as IDisposable).Dispose();
				}

				Clear();
			}
		}

		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
        protected override void RemoveItem(int index)
        {
			lock (lockObj) {
				IGameComponent gameComponent = base[index];
				base.RemoveItem(index);
				ComponentRemoved?.Invoke(this, new GameComponentCollectionEventArgs(gameComponent));
			}
        }

			
		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="item"></param>
        protected override void SetItem(int index, IGameComponent item)
        {
            throw new NotSupportedException();
        }
    }
}
