using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Binding {
	class ValueBindingException : Exception {

		public ValueBindingException ()
		{
		}
		
		public ValueBindingException ( string message ) : base( message )
		{
		}

		public ValueBindingException ( string format, params object[] args ) : base( string.Format(format, args) )
		{
			
		}

		public ValueBindingException ( string message, Exception inner ) : base( message, inner )
		{
		}
	}
}
