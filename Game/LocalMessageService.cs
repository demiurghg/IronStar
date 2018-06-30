using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Engine.Common;

namespace IronStar {

	class LocalMessageService : IMessageService {

		public void Push( string message )
		{
			Log.Message("MSG: {0}", message);
		}

		public void Push( Guid client, string message )
		{
			Log.Message("MSG: {0} {1}", client, message);
		}
	}
}
