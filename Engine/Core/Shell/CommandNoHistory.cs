using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Shell {
	public abstract class CommandNoHistory : ICommand {
		public abstract object Execute();
		public void	Rollback() {}
		public bool	IsHistoryOn() { return false; }
	}
}
