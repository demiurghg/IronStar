using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Widgets.Advanced
{
	[AttributeUsage(AttributeTargets.Property|AttributeTargets.Method)]
	public class AECategoryAttribute : Attribute 
	{
		public readonly string Category;

		public AECategoryAttribute( string category ) 
		{
			this.Category = category;
		}
	}
}
