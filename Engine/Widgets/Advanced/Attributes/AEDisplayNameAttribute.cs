using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Widgets.Advanced
{
	[AttributeUsage(AttributeTargets.Property|AttributeTargets.Method)]
	public class AEDisplayNameAttribute : Attribute 
	{
		public readonly string Name;

		public AEDisplayNameAttribute( string name ) 
		{
			this.Name = name;
		}
	}
}
