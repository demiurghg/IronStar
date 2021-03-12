using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Widgets.Binding
{
	public interface INameProvider
	{
		string GetDisplayName( object item );
	}
}
