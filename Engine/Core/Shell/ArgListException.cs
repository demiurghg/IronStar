using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Shell
{
	[Serializable]
	public class ArgListException : Exception {

		public ArgListException ()
		{
		}
		
		public ArgListException ( string message ) : base( message )
		{
		}

		public ArgListException ( string format, params object[] args ) : base( string.Format(format, args) )
		{
			
		}

		public ArgListException ( string message, Exception inner ) : base( message, inner )
		{
		}
	}
}
