using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Shell {
	public interface ICommand {
		void Execute();
		void Rollback();
		bool IsRollbackable { get; }
		object Result { get; }
	}
}
