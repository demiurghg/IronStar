using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;

namespace Fusion.Engine.Common {
	public interface IEditorInstance : IDisposable {
		void Initialize ();
		void Update ( GameTime gameTime );
	}
}
