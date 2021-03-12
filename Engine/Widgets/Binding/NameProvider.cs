using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;

namespace Fusion.Widgets.Binding
{
	public class NameProvider : INameProvider
	{
		readonly Func<object,string> converter = null;

		public NameProvider()
		{
		}

		public NameProvider(Func<object,string> converter)
		{
			this.converter	=	converter;
		}

		public string GetDisplayName( object item )
		{
			if (item==null)
			{
				return "(null)";
			}
			else
			{
				return converter==null ? item.ToString() : converter( item );
			}
		}
	}
}
