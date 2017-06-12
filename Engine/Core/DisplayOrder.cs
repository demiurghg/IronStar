using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core {

	/// <summary>
	/// Shell command attribute
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	public sealed class DisplayOrder : Attribute {

		/// <summary>
		/// Command name
		/// </summary>
		public int Order { get; private set; }

		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		public DisplayOrder ( int order )
		{
			Order	=	order;
		}
	}
}
