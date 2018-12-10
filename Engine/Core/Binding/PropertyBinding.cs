using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Fusion.Core.Binding {
	public class PropertyBinding : IValueBinding {

		readonly object targetObject;
		readonly PropertyInfo propertyInfo;


		public PropertyBinding ( object targetObject, PropertyInfo propertyInfo )
		{
			if (targetObject==null) {
				throw new ArgumentNullException( "targetObject" );
			}
			
			if (propertyInfo==null) {
				throw new ArgumentNullException( "propertyInfo" );
			}
			
			this.targetObject	=	targetObject;
			this.propertyInfo	=	propertyInfo;

			if (!propertyInfo.CanRead) {
				throw new ValueBindingException("Property '{0}' can not be read", propertyInfo.Name );
			}
		}


		public PropertyBinding ( object targetObject, string propertyName )
		{
			if (targetObject==null) {
				throw new ArgumentNullException( "targetObject" );
			}
			
			if (propertyInfo==null) {
				throw new ArgumentNullException( "propertyName" );
			}
			
			this.targetObject	=	targetObject;
			this.propertyInfo	=	targetObject.GetType().GetProperty( propertyName );

			if (propertyInfo==null) {
				throw new ValueBindingException("Property '{0}' was not found", propertyName );
			}

			/*if (!propertyInfo.PropertyType.IsAssignableFrom(typeof(TValue))) {
				throw new ValueBindingException("Type of property '{0}' and {1} is not assignable to each other", propertyName, typeof(TValue) );
			}

			if (!typeof(TValue).IsAssignableFrom(propertyInfo.PropertyType)) {
				throw new ValueBindingException("Type of property '{0}' and {1} is not assignable to each other", propertyName, typeof(TValue) );
			} */

			if (!propertyInfo.CanRead) {
				throw new ValueBindingException("Property '{0}' can not be read", propertyInfo.Name );
			}
		}


		public bool IsReadonly {
			get {
				return propertyInfo.CanWrite;
			}
		}


		public object GetValue()
		{
			return propertyInfo.GetValue(targetObject);
		}


		public bool SetValue(object value)
		{
			if (IsReadonly) {
				return false;
			} else {
				propertyInfo.SetValue(targetObject, value);
				return true;
			}
		}
	}
}
