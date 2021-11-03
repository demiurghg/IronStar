using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Fusion.Core;
using Fusion;

namespace IronStar.ECS
{
	class SystemWrapper
	{
		public readonly ISystem System;
		public readonly long Bit; 
		public readonly Aspect Aspect;
		readonly GameState gs;
		readonly Stopwatch stopwatch;
		TimeSpan profilingTime;

		public TimeSpan ProfilingTime { get { return profilingTime; } }
		
		public SystemWrapper( GameState gs, ISystem system, int index )
		{
			if (system==null) throw new ArgumentNullException(nameof(system));
			if (index>=GameState.MaxSystems || index<0) throw new ArgumentOutOfRangeException(nameof(index));

			this.gs		=	gs;
			this.System	=	system;
			this.Bit	=	1L << index;
			this.Aspect	=	system.GetAspect();

			this.stopwatch	=	new Stopwatch();
		}


		public override string ToString()
		{
			return string.Format("[{0}, {1}]", System.GetType().Name, Bit);
		}


		public void Update( GameState gs, GameTime gameTime )
		{									
			stopwatch.Reset();
			stopwatch.Start();

			using ( new CVEvent( System.GetType().Name ) )
			{
				System.Update( gs, gameTime );
			}

			stopwatch.Stop();
			profilingTime	=	stopwatch.Elapsed;
		}


		public void Changed( Entity entity )
		{
			bool contains	= (Bit & entity.SystemMapping) == Bit;
			bool accept		= Aspect.Accept(entity);

			if (accept && !contains)
			{
				entity.SystemMapping |= Bit;
				System.Add(gs, entity);
			}
			
			if (!accept && contains)
			{
				entity.SystemMapping &= ~Bit;
				System.Remove(gs, entity);
			}
		}
	}
}
