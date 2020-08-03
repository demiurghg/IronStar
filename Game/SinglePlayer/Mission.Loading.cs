using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.Core;
using Fusion.Core.Content;
using IronStar.Mapping;
using Fusion.Engine.Client;
using Fusion;

namespace IronStar.SinglePlayer {
	
	partial class Mission {

		class Loading : IMissionState {

			public bool IsContinuable {
				get { return false;	}
			}

			public MissionState State {
				get { return MissionState.Loading; }
			}



			readonly IContentPrecacher precacher;
			readonly Task loadingTask;
			readonly MissionContext context;
			DateTime startLoadingTime;


			public Loading ( Mission mission, string mapname )
			{
				startLoadingTime	=	DateTime.Now;

				context			=	new MissionContext( mission, mapname );

				precacher		=	new GameWorld.Precacher( context.Content, mapname );
				loadingTask		=	new Task( ()=> precacher.LoadContent() );
				loadingTask.Start();
				
			}


			public void Continue() { Log.Message("Mission.Loading : in progress."); }
			public void Exit() {}
			public void Pause() {}
			public void Start( string map )	{}


			public void Update( GameTime gameTime )
			{
				if (loadingTask.IsCompleted) 
				{
					var loadingTime = DateTime.Now - startLoadingTime;
					Log.Message("Loading completed : {0}", loadingTime);

					if (loadingTask.IsFaulted) 
					{
						Log.Warning("-------- Precache error --------");
						Log.Warning("{0}", loadingTask.Exception);
						Log.Warning("");
						Log.Warning("Precache skipped.");
						Log.Warning("--------------------------------");
					}

					context.Mission.State = new Waiting( context );
				}
			}
		}
	}
}
