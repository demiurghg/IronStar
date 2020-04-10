using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Scripting {

	/// <summary>
	/// Shell command attribute
	/// </summary>
	[AttributeUsage(AttributeTargets.Method|AttributeTargets.Property)]
	public sealed class LuaApiAttribute : Attribute {

		public readonly string Name;

		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		public LuaApiAttribute ( string name )
		{
			this.Name = name;
		}
	}
}
