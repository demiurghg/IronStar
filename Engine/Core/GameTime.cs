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
		public static TimeSpan CurrentTime { get { return sw.Elapsed; } }
		static readonly Stopwatch sw = new Stopwatch();
		static GameTime() { sw.Start(); }

		static public GameTime Zero   { get { return new GameTime( CurrentTime, TimeSpan.Zero, 0 ); } }
		static public GameTime MSec16 { get { return new GameTime( CurrentTime, TimeSpan.FromMilliseconds(16), 1 ); } }
		static public GameTime MSec1  { get { return new GameTime( CurrentTime, TimeSpan.FromMilliseconds( 1), 1 ); } }
		static public GameTime Bad    { get { throw new NotImplementedException(); } }

		static public GameTime Start()
		{
			return new GameTime( CurrentTime, TimeSpan.Zero, 0 );
		}

		readonly TimeSpan	current;
		readonly TimeSpan	elapsed;
		readonly long		frames;

		public GameTime( TimeSpan elapsed, long frames )
		{
			this.current	=	CurrentTime;
			this.elapsed	=	elapsed;
			this.frames		=	frames;
		}

		private GameTime( TimeSpan current, TimeSpan elapsed, long frames )
		{
			this.current	=	current;
			this.elapsed	=	elapsed;
			this.frames		=	frames;
		}

		public	GameTime Next()
		{
			var currentTime = CurrentTime;
			var elapsedTime = currentTime - this.current;
			return new GameTime( currentTime, elapsedTime, frames + 1 );
		}

		public long Frames { get { return frames; } }

		public TimeSpan Current { get { return current; }	}

		public TimeSpan Elapsed { get { return elapsed; } }

		public float ElapsedSec { get { return (float)elapsed.TotalSeconds; } }

		public int Milliseconds { get { return (int)(elapsed.TotalMilliseconds); } }

		public float Fps { get { return ElapsedSec==0 ? 0 : 1 / ElapsedSec; } }
	}
}
