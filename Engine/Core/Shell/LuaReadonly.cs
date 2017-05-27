using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Shell {

	/// <summary>
	/// Shell command attribute
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class LuaReadonly : Attribute {

		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		public LuaReadonly ()
		{
		}
	}
}
