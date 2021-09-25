using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Fusion.Core;

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
		
		public SystemWrapper( GameState gs, ISystem system )
		{
			if (system==null) throw new ArgumentNullException("system");

			this.gs		=	gs;
			this.System	=	system;
			this.Bit	=	ECSTypeManager.GetSystemBit( system.GetType() );
			this.Aspect	=	system.GetAspect();

			this.stopwatch	=	new Stopwatch();
		}


		public void Update( GameState gs, GameTime gameTime )
		{									
			stopwatch.Reset();
			stopwatch.Start();

			System.Update( gs, gameTime );

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
