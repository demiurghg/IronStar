using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Editor2.AttributeEditor {

	[Serializable]
	internal class AEException : Exception {

		public AEException ()
		{
		}
		
		public AEException ( string message ) : base( message )
		{
		}

		public AEException ( string format, params object[] args ) : base( string.Format(format, args) )
		{
			
		}

		public AEException ( string message, Exception inner ) : base( message, inner )
		{
		}
	}
}
