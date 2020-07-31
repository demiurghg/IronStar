using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	class SystemWrapper
	{
		public readonly ISystem System;
		public readonly long Bit; 
		public readonly Aspect Aspect;
		readonly GameState gs;

		
		public SystemWrapper( GameState gs, ISystem system )
		{
			if (system==null) throw new ArgumentNullException("system");

			this.gs		=	gs;
			this.System	=	system;
			this.Bit	=	ECSTypeManager.GetSystemBit( system.GetType() );
			this.Aspect	=	system.GetAspect();
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
