using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;

namespace IronStar.SinglePlayer {

	public interface IMissionState {

		/// <summary>
		/// Start mission
		/// </summary>
		/// <param name="map"></param>
		void Start ( string map );

		/// <summary>
		/// Continue mission, unpause etc.
		/// </summary>
		void Continue ();

		/// <summary>
		/// Exit mission
		/// </summary>
		void Exit ();

		/// <summary>
		/// Pause mission
		/// </summary>
		void Pause ();

		/// <summary>
		/// Update mission state, check trtiggers, 
		/// update player load stuff etc/
		/// </summary>
		/// <param name="gameTime"></param>
		void Update ( GameTime gameTime );

		/// <summary>
		/// Indicates, whether current state is continuable.
		/// 
		/// </summary>
		bool IsContinuable { get; }

		/// <summary>
		/// Gets mission state
		/// </summary>
		MissionState State { get; }
	}
}
