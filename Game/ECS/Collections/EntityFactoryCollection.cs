﻿using System;
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
	class EntityFactoryCollection : Dictionary<string,EntityFactory>
	{
		public EntityFactoryCollection() : base( 
				Misc.GetAllClassesWithAttribute<EntityFactoryAttribute>()
					.ToDictionary(
						t0 => t0.GetAttribute<EntityFactoryAttribute>().ClassName,
						t1 => (EntityFactory)Activator.CreateInstance( t1 )
					), StringComparer.OrdinalIgnoreCase )
		{
		}
	}
}
