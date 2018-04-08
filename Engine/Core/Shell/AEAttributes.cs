using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Shell {

	[AttributeUsage(AttributeTargets.Property|AttributeTargets.Method)]
	public class AECategoryAttribute : Attribute {

		public readonly string Category;

		public AECategoryAttribute( string category ) 
		{
			this.Category = category;
		}
	}


	[AttributeUsage(AttributeTargets.Property|AttributeTargets.Method)]
	public class AEDisplayNameAttribute : Attribute {

		public readonly string Name;

		public AEDisplayNameAttribute( string name ) 
		{
			this.Name = name;
		}
	}


	[AttributeUsage(AttributeTargets.Method)]
	public class AECommandAttribute : Attribute {
	}


	[AttributeUsage(AttributeTargets.Property)]
	public class AEValueRangeAttribute : Attribute {

		public readonly float Min;
		public readonly float Max;
		public readonly float RoughStep;
		public readonly float PreciseStep;

		public AEValueRangeAttribute( float min, float max, float roughStep, float preciseStep ) 
		{
			this.Min			=	min;
			this.Max			=	max;
			this.RoughStep		=	roughStep;
			this.PreciseStep	=	preciseStep;
		}
	}
}
