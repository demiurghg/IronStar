using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Drivers.Graphics;
using Fusion.Core;
using Fusion.Engine.Tools;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;
using System.Threading;


namespace Fusion.Engine.Common {

	public abstract class GameComponent : DisposableBase, IGameComponent, IUpdatable {

		/// <summary>
		/// Gets Game object associated with given component.
		/// Lower values are updated first.
		/// </summary>
		public Game Game { 
			get; 
			private set; 
		}

		/// <summary>
		/// Indicates whether the game component's 
		/// Update method should be called in Game.Update.
		/// </summary>
		public bool Enabled {
			get {
				throw new NotImplementedException();
			}
			set {
				if (enabled!=value) {
					enabled = value;
					EnabledChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}
		bool enabled;

		/// <summary>
		/// Indicates when the game component should be updated relative to other game components. 
		/// Lower values are updated first.
		/// </summary>
		public int UpdateOrder {
			get {
				throw new NotImplementedException();
			}
			set {
				if (updateOrder!=value) {
					updateOrder = value;
					UpdateOrderChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}
		int updateOrder;


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="game"></param>
		public GameComponent ( Game game )
		{
			this.Game = game;
		}

		public event EventHandler<EventArgs> EnabledChanged;
		public event EventHandler<EventArgs> UpdateOrderChanged;


		/// <summary>
		/// Called when the component should be initialized. 
		/// This method can be used for tasks like querying for services the component 
		/// needs and setting up resources.
		/// </summary>
		public virtual void Initialize ()
		{
		}


		/// <summary>
		/// Indicates whether the game component's 
		/// Update method should be called in Game.Update.
		/// </summary>
		/// <param name="gameTime"></param>
		public virtual void Update( GameTime gameTime )
		{
		}
	}
}
