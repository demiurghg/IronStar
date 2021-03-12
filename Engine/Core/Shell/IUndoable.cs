using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Shell 
{
	public interface IUndoable : ICommand
	{
		void Rollback();
	}
}
