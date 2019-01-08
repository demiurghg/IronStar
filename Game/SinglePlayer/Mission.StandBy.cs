using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion;

namespace IronStar.SinglePlayer {

	partial class Mission {

		class StandBy : IMissionState {

			public bool IsContinuable {
				get {
					return false;
				}
			}


			public MissionState State {
				get {
					return MissionState.StandBy;
				}
			}

			readonly Game game;
			readonly Mission mission;


			public StandBy ( Mission mission )
			{
				this.game		=	mission.Game;
				this.mission	=	mission;
			}


			public void Continue()
			{
				Log.Warning("Mission.StandBy.Continue is not implemented");
			}


			public void Exit()
			{
				game.Exit();
			}

			
			public void Pause()
			{
			}

			
			public void Start( string map )
			{
				mission.State	=	new Loading( mission, map );
			}


			public void Update( GameTime gameTime )
			{
				
			}
		}
	}
}
