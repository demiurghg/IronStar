using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core {

	/// <summary>
	/// Shell command attribute
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class CommandAttribute : Attribute {

		/// <summary>
		/// Command name
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Command name
		/// </summary>
		public int GroupID { get; private set; }

		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		public CommandAttribute ( string name, int groupId = 0 )
		{
			this.Name		=	name;
			this.GroupID	=	groupId;
		}
	}
}
