using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Client;
using Fusion.Engine.Server;

namespace Fusion.Engine.Common {
	public interface IGameFactory {
		IClientInstance	CreateClient ( Game game, IMessageService msgsvc, Guid clientGuid );
		IServerInstance CreateServer ( Game game, IMessageService msgsvc, string map, string options );
		IUserInterface	CreateUI ( Game game );	 
		IEditorInstance	CreateEditor ( Game game, string map );
	}
}
