using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Shell {

	public class Suggestion {

		List<string> candidates = new List<string>(16);

		public string CommandLine { get; set; }

		public IEnumerable<string> Candidates { get { return candidates; } }

		public Suggestion ( string cmdline ) 
		{
			CommandLine = cmdline;
		}

		public void Set ( string cmdline )
		{
			CommandLine = cmdline;
		}

		public void Add ( string candidate ) 
		{
			candidates.Add( candidate );
		}

		public void Clear () 
		{
			candidates.Clear();
		}

		public void AddRange ( IEnumerable<string> more ) 
		{
			candidates.AddRange( more );
		}
	}
}
