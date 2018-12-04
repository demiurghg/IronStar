using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Engine.Common;

namespace SpaceMarines {

	/// <summary>
	/// Makes class visible for uberfactory
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class FactoryAttribute : Attribute {

		public readonly bool IncludeSubclasses;

		public FactoryAttribute(bool includeSubclasses)
		{
			this.IncludeSubclasses = includeSubclasses;
		}
	}
}
