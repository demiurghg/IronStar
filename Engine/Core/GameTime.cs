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

	public class GameTime2 
	{
		/// <summary>
		/// Averaging frame count.
		/// </summary>

		Stopwatch		stopWatch;
		TimeSpan		total;
		TimeSpan		elapsed;
		long			frame;

		/// <summary>
		/// Gets total number of frames since game had been started.
		/// </summary>
		public long Frames { get { return frame; } }

		/// <summary>
		/// Total game time since game had been started.
		/// </summary>
		public	TimeSpan Total { get { return total; }	}

		/// <summary>
		/// Elapsed time since last frame.
		/// </summary>
		public	TimeSpan Elapsed { get { return elapsed; } }

		/// <summary>
		/// Elapsed time in seconds since last frame.
		/// </summary>
		public	float ElapsedSec { get { return (float)elapsed.TotalSeconds; } }

		/// <summary>
		/// Elapsed time in milliseconds
		/// </summary>
		public int Milliseconds { get { return (int)(elapsed.TotalMilliseconds); } }

		/// <summary>
		/// Frames per second.
		/// </summary>
		public float Fps { get { return 1 / ElapsedSec; } }


		public static GameTime2 Zero {
			get {
				return new GameTime2( 0, new TimeSpan(0,0,0,0,0), new TimeSpan(0,0,0,0,0) );
			}
		}

		
		/// <summary>
		/// Constructor
		/// </summary>
		public GameTime2 ()
		{
			stopWatch	= new Stopwatch();
			stopWatch.Start();
			total		= stopWatch.Elapsed;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="total"></param>
		/// <param name="elapsed"></param>
		internal GameTime2 ( long frames, TimeSpan total, TimeSpan elapsed )
		{
			this.frame		=	frames;
			this.total		=	total;
			this.elapsed	=	elapsed;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="total"></param>
		/// <param name="elapsed"></param>
		internal GameTime2 ( long frames, long totalTicks, long elapsedTicks )
		{
			this.frame		=	frames;
			this.total		=	new TimeSpan(totalTicks);
			this.elapsed	=	new TimeSpan(elapsedTicks);
		}


		/// <summary>
		/// Updates timer
		/// </summary>
		public void Update()
		{
			if (stopWatch==null) {
				throw new InvalidOperationException("Do not update GameTime created with explicit values.");
			}

			if (false)
			{
				elapsed         =   new TimeSpan( (long)( (TimeSpan.TicksPerMillisecond * 1000) / 60 / 3) );
				total           +=  elapsed;
			}
			else
			{
				var newTotal    =   stopWatch.Elapsed;
				elapsed         =   newTotal - total;
				total           =   newTotal;
			}
		}
	}
}
