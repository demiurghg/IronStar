using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Fusion.Widgets.Binding 
{
	public delegate void PropertyChangeHandler(object target, PropertyInfo property, ValueSetMode mode, object value);

	public class PropertyBinding : IValueBinding 
	{
		readonly object targetObject;
		readonly PropertyInfo propertyInfo;
		readonly PropertyChangeHandler onChanging;
		readonly PropertyChangeHandler onChanged;

		public PropertyBinding ( object targetObject, PropertyInfo propertyInfo ) : this( targetObject, propertyInfo, null, null ) {}

		
		public PropertyBinding ( object targetObject, PropertyInfo propertyInfo, PropertyChangeHandler changing, PropertyChangeHandler changed  )
		{
			if (targetObject==null) 
			{
				throw new ArgumentNullException( "targetObject" );
			}
			
			if (propertyInfo==null) 
			{
				throw new ArgumentNullException( "propertyInfo" );
			}
			
			this.onChanged		=	changed;
			this.onChanging		=	changing;
			this.targetObject	=	targetObject;
			this.propertyInfo	=	propertyInfo;

			if (!propertyInfo.CanRead) 
			{
				throw new ValueBindingException("Property '{0}' can not be read", propertyInfo.Name );
			}
		}


		public bool IsReadonly 
		{
			get { return !propertyInfo.CanWrite; }
		}


		public Type ValueType
		{
			get { return propertyInfo.PropertyType; }
		}


		public object GetValue()
		{
			return propertyInfo.GetValue(targetObject);
		}


		public bool SetValue(object value, ValueSetMode mode)
		{
			if (IsReadonly) 
			{
				return false;
			}
			else 
			{
				onChanging?.Invoke(targetObject, propertyInfo, mode, value );

				propertyInfo.SetValue(targetObject, value);

				onChanged?.Invoke(targetObject, propertyInfo, mode, value );

				return true;
			}
		}
	}
}
