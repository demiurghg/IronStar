using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;

namespace Fusion.Core.Shell
{
	[Serializable]
	public class InvokerException : Exception {

		public InvokerException ()
		{
		}
		
		public InvokerException ( string message ) : base( message )
		{
		}

		public InvokerException ( string format, params object[] args ) : base( string.Format(format, args) )
		{
			
		}

		public InvokerException ( string message, Exception inner ) : base( message, inner )
		{
		}
	}
}
