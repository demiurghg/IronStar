using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Fusion.Widgets.Binding 
{
	public delegate void PropertyChangeHandler(object target, PropertyInfo property, object value);

	public class PropertyBinding : IValueBinding 
	{
		readonly object targetObject;
		readonly PropertyInfo propertyInfo;
		readonly PropertyChangeHandler onInitiate;
		readonly PropertyChangeHandler onChange;
		readonly PropertyChangeHandler onCommit;
		readonly PropertyChangeHandler onCancel;
		object storedValue;
		object newValue;

		public PropertyBinding ( object targetObject, PropertyInfo propertyInfo ) : this( targetObject, propertyInfo, null, null, null, null ) {}

		
		public PropertyBinding ( object targetObject, PropertyInfo propertyInfo, PropertyChangeHandler initiate, PropertyChangeHandler change, PropertyChangeHandler commit, PropertyChangeHandler cancel )
		{
			if (targetObject==null) 
			{
				throw new ArgumentNullException( "targetObject" );
			}
			
			if (propertyInfo==null) 
			{
				throw new ArgumentNullException( "propertyInfo" );
			}
			
			this.onInitiate		=	initiate;
			this.onChange		=	change;
			this.onCommit		=	commit;
			this.onCancel		=	cancel;
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


		public void Initiate()
		{
			storedValue = propertyInfo.GetValue( targetObject );
			onInitiate?.Invoke( targetObject, propertyInfo, storedValue );
		}


		public void Commit()
		{
			onCommit?.Invoke( targetObject, propertyInfo, newValue );
		}


		public void Cancel()
		{
			onCancel?.Invoke( targetObject, propertyInfo, storedValue );
		}


		public bool SetValue(object value)
		{
			if (IsReadonly) 
			{
				return false;
			}
			else 
			{
				newValue = value;
				propertyInfo.SetValue(targetObject, value);
				onChange?.Invoke(targetObject, propertyInfo, value);
				return true;
			}
		}
	}
}
