using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Client;
using Fusion.Engine.Common;

namespace Fusion.Engine.Campaign {
	interface ISinglePlayerInstance {

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mapInfo"></param>
		void Initialize ( string mapInfo );

		/// <summary>
		/// Creates async loader for single-player game instance
		/// </summary>
		/// <param name="mapInfo"></param>
		/// <returns></returns>
		IContentPrecacher CreatePrecacher ( string mapInfo );

		/// <summary>
		/// Updates single-player game state
		/// </summary>
		/// <param name="gameTime"></param>
		void Update ( GameTime gameTime );
	}
}
