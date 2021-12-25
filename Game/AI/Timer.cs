using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core;

namespace IronStar.AI
{
	public struct Timer
	{
		private int counter;

		public Timer( int msec )
		{
			counter	=	msec;
		}

		public Timer( int msecMin, int msecMax )
		{
			counter = MathUtil.Random.Next( msecMin, msecMax );
		}

		public void Set( int msec )
		{
			counter = msec;
		}

		public void SetND( int msec )
		{
			counter = Math.Max(msec/6, (int)MathUtil.Random.GaussDistribution( msec, msec/3 ));
		}

		public void Set( int msecMin, int msecMax )
		{
			counter = MathUtil.Random.Next( msecMin, msecMax );
		}

		public void Update ( GameTime gameTime )
		{
			counter -= gameTime.Milliseconds;
		}

		public bool IsElapsed
		{
			get { return counter <= 0; }
		}
	}
}
