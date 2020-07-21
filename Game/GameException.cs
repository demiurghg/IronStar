using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using Native.NvApi;
using System.IO;


namespace IronStar 
{
	[Serializable]
	internal class GameException : Exception 
	{
		public GameException ()
		{
		}
		
		public GameException ( string message ) : base( message )
		{
		}

		public GameException ( string format, params object[] args ) : base( string.Format(format, args) )
		{
			
		}

		public GameException ( string message, Exception inner ) : base( message, inner )
		{
		}
	}
}
