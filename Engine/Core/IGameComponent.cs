using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core {
	public interface IGameComponent {

		/// <summary>
		/// Called when the component should be initialized. 
		/// This method can be used for tasks like querying for services the component 
		/// needs and setting up resources.
		/// </summary>
		void Initialize();
	}
}
