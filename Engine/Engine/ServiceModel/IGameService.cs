using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;

namespace Fusion.Engine.ServiceModel {

	public interface IGameService : IDisposable {
		void Initialize ();	
		void Update ( GameTime gameTime );
	}
}
