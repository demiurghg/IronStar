using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Engine.Common {
	public interface IMessageService {
		void Push ( Guid client, string message );
		void Push ( string message );
	}
}
