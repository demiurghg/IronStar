using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;

namespace IronStar.SinglePlayer {
	
	partial class Mission {

		class Debriefing : IMissionState {

			public bool IsContinuable {
				get {
					throw new NotImplementedException();
				}
			}

			public MissionState State {
				get {
					return MissionState.Debriefing;
				}
			}

			public void Continue()
			{
				throw new NotImplementedException();
			}

			public void Exit()
			{
				throw new NotImplementedException();
			}

			public void Pause()
			{
				throw new NotImplementedException();
			}

			public void Start( string map )
			{
				throw new NotImplementedException();
			}

			public void Update( GameTime gameTime )
			{
				throw new NotImplementedException();
			}
		}
	}
}
