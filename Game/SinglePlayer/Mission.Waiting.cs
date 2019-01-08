using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Content;

namespace IronStar.SinglePlayer {

	partial class Mission {

		class Waiting : IMissionState {

			public bool IsContinuable { 
				get { throw new NotImplementedException(); }
			}

			public MissionState State {
				get { return MissionState.Waiting; }
			}


			readonly MissionContext context;


			public Waiting( MissionContext context )
			{
				this.context	=	context;
			}


			public void Continue()
			{
				context.Mission.State	=	new Active( context );
			}


			public void Exit() {}
			public void Pause() {}
			public void Start( string map ) {}
			public void Update( GameTime gameTime ) {}
		}
	}
}
