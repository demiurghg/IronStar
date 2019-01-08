using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.SinglePlayer {
	public enum MissionState {
		StandBy,
		Loading,
		Waiting,
		Briefing,
		Active,
		Paused,
		CutScene,
		Debriefing,
	}
}
