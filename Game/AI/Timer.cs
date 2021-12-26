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
		private int timeout;

		public Timer( int msec )
		{
			timeout	=	msec;
			counter	=	timeout;
		}

		public Timer( int msecMin, int msecMax )
		{
			timeout	=	MathUtil.Random.Next( msecMin, msecMax );
			counter	=	timeout;
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

		public float Fraction
		{
			get { return MathUtil.Clamp( counter / (float)timeout, 0, 1); }
		}

		public bool IsElapsed
		{
			get { return counter <= 0; }
		}
	}
}
