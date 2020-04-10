using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Shell {
	public interface ISyntax {
		string	 Description { get; }
		string	 Usage { get; }
		string[] Required( int index, string arg );
		string[] Optional( string arg );
	}
}
