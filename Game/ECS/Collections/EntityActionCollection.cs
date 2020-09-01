using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using System.Globalization;
using System.Collections;
using Fusion.Core.Extensions;

namespace IronStar.ECS
{
	class EntityActionCollection : Dictionary<string,EntityAction>
	{
		public EntityActionCollection() : base(
				Fusion.Core.Extensions.Misc.GetAllClassesWithAttribute<EntityActionAttribute>()
					.ToDictionary(
						t0 => t0.GetAttribute<EntityActionAttribute>().ActionName,
						t1 => (EntityAction)Activator.CreateInstance( t1 )
					), StringComparer.OrdinalIgnoreCase )
		{
			
		}
	}
}
