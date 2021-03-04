using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Widgets.Binding;

namespace Fusion.Core.Shell {

	[Obsolete]
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
