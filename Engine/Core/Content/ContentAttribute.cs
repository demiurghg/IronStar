using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Content
{
	class ContentAttribute : Attribute
	{
		public readonly string Name;

		public ContentAttribute( string name )
		{
			Name = name;
		}
	}
}
