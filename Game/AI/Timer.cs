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
	public class Timer
	{
		private int counter;
		private int timeout;

		public int Timeout { get { return timeout; } }

		public Timer()
		{
		}

		public Timer( int msec )
		{
			Set( msec );
		}

		public Timer( int msecMin, int msecMax )
		{
			Set( msecMin, msecMax );
		}

		public void Set( int msec )
		{
			timeout	=	msec;
			counter	=	timeout;
		}

		public void SetND( int msec )
		{
			timeout	=	Math.Max(msec/6, (int)MathUtil.Random.GaussDistribution( msec, msec/3 ));
			counter	=	timeout;
		}

		public void Set( int msecMin, int msecMax )
		{
			timeout	=	MathUtil.Random.Next( msecMin, msecMax );
			counter	=	timeout;
		}

		public void Update ( GameTime gameTime )
		{
			counter -= gameTime.Milliseconds;
		}

		public void Update ( int msec )
		{
			counter -= msec;
		}

		public float Fraction
		{
			get 
			{ 
				if (timeout==0) return 0;
				return MathUtil.Clamp( counter / (float)timeout, 0, 1); 
			}
		}

		public bool IsElapsed
		{
			get { return counter <= 0; }
		}
	}
}
