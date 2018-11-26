using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Fusion.Core;
using System.Threading.Tasks;

namespace Fusion.Engine.Common {
	public interface IUserInterface : IDisposable, IExitable {

		/// <summary>
		/// 
		/// </summary>
		void Initialize ();

		/// <summary>
		/// Called when the game has determined that UI logic needs to be processed.
		/// </summary>
		/// <param name="gameTime"></param>
		void Update ( GameTime gameTime );

		/// <summary>
		/// This method called each time when discovery responce arrived.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="serverInfo"></param>
		void DiscoveryResponse ( IPEndPoint endPoint, string serverInfo );
	}
}
