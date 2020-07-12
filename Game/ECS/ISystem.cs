using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	public interface ISystem
	{
		void Process ( Entity entity );
	}
}
