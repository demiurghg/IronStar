using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	interface IEntityTemplate
	{
		void Spawn( Entity entity, string parameters );
	}
}
