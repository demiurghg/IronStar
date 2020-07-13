using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	class SystemCollection : FixedList<ISystem>
	{
		const int MaxSystems = (int)BitSet.MaxBits;

		public SystemCollection() : base(MaxSystems)
		{
		}
	}
}
