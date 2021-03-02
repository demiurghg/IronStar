using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Shell
{
	public abstract class AEValueProviderAttribute : Attribute 
	{
		public abstract string[] GetValues ( Game game );
	}
}
