using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core {
	public interface IExitable {

		/// <summary>
		/// Called when user tries to close program using Alt-F4 or from windows menu.
		/// </summary>
		void RequestToExit();
	}
}
