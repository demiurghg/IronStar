using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Fusion.Core.Mathematics;

namespace Fusion.Core 
{
	public class GameTime
	{
		public static TimeSpan Current { get { return sw.Elapsed; } }
		static readonly Stopwatch sw = new Stopwatch();
		static GameTime() { sw.Start(); }

		static public GameTime Zero   { get { return new GameTime( Current, TimeSpan.Zero, 0 ); } }
		static public GameTime MSec16 { get { return new GameTime( Current, TimeSpan.FromMilliseconds(16), 1 ); } }
		static public GameTime MSec1  { get { return new GameTime( Current, TimeSpan.FromMilliseconds( 1), 1 ); } }
		static public GameTime Bad    { get { throw new NotImplementedException(); } }

		static public GameTime Start()
		{
			return new GameTime( Current, TimeSpan.Zero, 0 );
		}

		readonly TimeSpan	total;
		readonly TimeSpan	elapsed;
		readonly long		frames;

		private GameTime( TimeSpan total, TimeSpan elapsed, long frames )
		{
			this.total		=	total;
			this.elapsed	=	elapsed;
			this.frames		=	frames;
		}

		public	GameTime Next()
		{
			var current = Current;
			var elapsed = current - total;
			return new GameTime( current, elapsed, frames + 1 );
		}

		public long Frames { get { return frames; } }

		public TimeSpan Total { get { return total; }	}

		public TimeSpan Elapsed { get { return elapsed; } }

		public float ElapsedSec { get { return (float)elapsed.TotalSeconds; } }

		public int Milliseconds { get { return (int)(elapsed.TotalMilliseconds); } }

		public float Fps { get { return ElapsedSec==0 ? 0 : 1 / ElapsedSec; } }
	}
}
