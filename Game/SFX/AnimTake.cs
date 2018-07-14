using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.SFX {

	public class AnimFX {
		public string	Joint;
		public int		Frame;
		public short	FX;
	}

	public class AnimTransition {
		
		public int MinFrame;
		public int MaxFrame;

	}

	public class AnimTake {

		public string	Name;
		public string	State;

		public string	NextTake;
		public int		NextFrame;


	}
}
