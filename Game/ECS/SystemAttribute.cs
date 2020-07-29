using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	public enum SystemUpdateMode
	{
		UserInput,
		Simulation,
		GameLogic,
		Rendering,
	}

	
	public class SystemAttribute : Attribute
	{
		public readonly SystemUpdateMode UpdateMode;

		public SystemAttribute( SystemUpdateMode updateMode )
		{
			this.UpdateMode	=	updateMode;
		}
	}
}
