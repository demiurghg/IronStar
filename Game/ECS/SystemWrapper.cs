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

		public SystemWrapper( ISystem system )
		{
			if (system==null) throw new ArgumentNullException("system");

			this.System	=	system;
			this.Bit	=	ECSTypeManager.GetSystemBit( system.GetType() );
			this.Aspect	=	system.GetAspect();
		}
	}
}
