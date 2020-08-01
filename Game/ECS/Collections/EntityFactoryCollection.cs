using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using System.Globalization;
using System.Collections;

namespace IronStar.ECS
{
	class EntityFactoryCollection : Dictionary<string,EntityFactory>
	{
		public EntityFactoryCollection( Dictionary<string,EntityFactory> dict  ) : base( dict, StringComparer.OrdinalIgnoreCase )
		{
			
		}
	}
}
