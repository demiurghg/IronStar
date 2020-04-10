using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Fusion.Core.Binding {
	public class StringPropertyBinding : PropertyBinding<string> {

		public StringPropertyBinding ( object targetObject, PropertyInfo propertyInfo )	: base( targetObject, propertyInfo )
		{
		}

		public StringPropertyBinding ( object targetObject, string propertyName )	: base( targetObject, propertyName )
		{
		}
	}
}
