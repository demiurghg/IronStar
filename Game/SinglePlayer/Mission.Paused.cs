using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;

namespace IronStar.SinglePlayer {

	partial class Mission {

		class Paused : IMissionState {

			public bool IsContinuable {
				get { throw new NotImplementedException(); }
			}

			public MissionState State {
				get { return MissionState.Paused; }
			}


			MissionContext context;


			public Paused ( MissionContext context )
			{
				this.context = context;
			}


			public void Continue()
			{
				context.Mission.State = new Active( context );
			}

			public void Exit()
			{
				context.Content.Unload();
				context.GameWorld.Dispose();
				context.Mission.State = new StandBy( context.Mission );
			}

			void IMissionState.Pause() { }

			public void Start( string map )
			{
				Log.Warning("Mission.Paused.Start : not implemented");
			}

			public void Update( GameTime gameTime )
			{
				context.GameWorld.PresentWorld( gameTime, 1, context.Camera, context.Command );
			}
		}
	}
}
