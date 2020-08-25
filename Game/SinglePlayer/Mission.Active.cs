using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Lights;
using IronStar.Client;
using IronStar.Core;
using IronStar.ECS;
using IronStar.SFX;
using IronStar.Gameplay.Systems;

namespace IronStar.SinglePlayer {

	partial class Mission 
	{
		class Active : IMissionState 
		{
			public bool IsContinuable 
			{
				get { return false;	}
			}

			public MissionState State 
			{
				get { return MissionState.Active; }
			}



			MissionContext context;

			public Active ( MissionContext context )
			{
				this.context	=	context;
				var userGuid	=	context.UserGuid;

				if (context.GameState==null) {

					var map				=	context.Content.Load<Mapping.Map>(@"maps\" + context.MapName);
					var msgsvc			=	new LocalMessageService();

					context.GameState	=	IronStar.CreateGameState( context.Game, context.Content, context.MapName );
					context.Input		=	new GameInput( context.Game );
					context.Command		=	new UserCommand();

					/*var player			=	context.GameState.SpawnPlayer( userGuid, "Unnamed Player");
					context.Command.SetAnglesFromQuaternion( player.Rotation );*/
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
				context.GameState.Update( gameTime );

				/*context.Input.Update( gameTime, context.GameState, ref context.Command );

				context.GameState.FeedPlayerCommand( context.UserGuid, context.Command );
				context.GameState.SimulateWorld( gameTime );

				context.Camera.Update( gameTime, 0, context.Command );

				context.GameState.PresentWorld( gameTime, 0, context.Camera, context.Command );*/
			}
		}
	}
}
