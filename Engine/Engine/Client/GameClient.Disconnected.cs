using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using System.Threading;
using System.IO;
using Fusion.Engine.Common;
using Fusion.Core;
using Fusion.Engine.Server;
using System.Net;


namespace Fusion.Engine.Client {
	public partial class GameClient {

		class Disconnected : State {

			ClientContext context;

			public Disconnected ( ClientContext context, string reason ) : base(context.GameClient, ClientState.Disconnected)
			{
				this.context = context;
				Message	=	reason;
			}



			public override bool UserConnect ( string host, int port, IClientInstance clientInstance )
			{
				Log.Warning("Wait stand by.");
				return false;
			}



			public override void UserDisconnect ( string reason )
			{
				Log.Warning("Already disconnected.");
			}



			public override void Update ( GameTime gameTime )
			{
				context?.Dispose();

				//	fall immediatly to stand-by mode:
				gameClient.SetState( new StandBy( gameClient ) );
			}



			public override void StatusChanged(NetConnectionStatus status, string message, NetConnection connection)
			{							
				
			}


			public override void DataReceived ( NetCommand command, NetIncomingMessage msg )
			{
			}
		}
	}
}
