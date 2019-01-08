using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using IronStar.Client;
using IronStar.Core;
using IronStar.Views;

namespace IronStar.SinglePlayer {

	partial class Mission {

		class Active : IMissionState {

			public bool IsContinuable {
				get { return false;	}
			}

			public MissionState State {
				get { return MissionState.Active; }
			}



			MissionContext context;

			public Active ( MissionContext context )
			{
				this.context	=	context;
				var userGuid	=	context.UserGuid;

				if (context.GameWorld==null) {

					var map				=	context.Content.Load<Mapping.Map>(@"maps\" + context.MapName);
					var msgsvc			=	new LocalMessageService();

					context.GameWorld	=	new GameWorld( context.Game, map, context.Content, msgsvc, userGuid );

					context.Camera		=	new GameCamera( context.GameWorld, userGuid ); 
					context.Input		=	new GameInput( context.Game );
					context.Command		=	new UserCommand();

					var player			=	context.GameWorld.SpawnPlayer( userGuid, "Unnamed Player");
					context.Command.SetAnglesFromQuaternion( player.Rotation );

				}
			}


			public void Continue()
			{
			}


			public void Exit()
			{
				throw new NotImplementedException();
			}


			public void Pause()
			{
				context.Mission.State = new Paused( context );
			}


			public void Start( string map )
			{
				Log.Warning("Mission.Active.Start is not implemented");
			}


			public void Update( GameTime gameTime )
			{
				context.Input.Update( gameTime, context.GameWorld, ref context.Command );

				context.GameWorld.FeedPlayerCommand( context.UserGuid, context.Command );
				context.GameWorld.SimulateWorld( gameTime );

				context.Camera.Update( gameTime, 0, context.Command );

				context.GameWorld.PresentWorld( gameTime, 0, context.Camera, context.Command );
			}
		}
	}
}
