using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;

namespace IronStar.SinglePlayer 
{
	public partial class Mission : GameComponent 
	{

		/// <summary>
		/// Sets and gets current mission state.
		/// </summary>
		public IMissionState State {
			get { return state; }
			protected set { 
				if (state!=value) {
					state = value;
					Log.Message("Mission state changed : {0}", state.GetType().Name );
					MissionStateChanged?.Invoke( this, new MissionEventArgs(state.State) );
				}
			}
		}

		IMissionState state;

		public event EventHandler<MissionEventArgs> MissionStateChanged;

		public class MissionEventArgs : EventArgs {
			public MissionEventArgs( MissionState state ) {
				State = state;
			}
			public readonly MissionState State;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public Mission( Game game ) : base( game )
		{
		}


		/// <summary>
		/// 
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();
			State	=	new StandBy( this );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update( GameTime gameTime )
		{
			base.Update( gameTime );

			state.Update( gameTime );
		}
	}
}
