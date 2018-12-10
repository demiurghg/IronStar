using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Binding {
	public class DelegateBinding<TValue> : IValueBinding {

		readonly Func<TValue> getFunc;
		readonly Action<TValue> setFunc;

		
		public DelegateBinding ( Func<TValue> getFunc, Action<TValue> setFunc )
		{
			this.getFunc	=	getFunc;
			this.setFunc	=	setFunc;
		}


		public DelegateBinding ( Func<TValue> getFunc )
		{
			this.getFunc	=	getFunc;
			this.setFunc	=	null;
		}


		public bool IsReadonly {
			get {
				return setFunc==null;
			}
		}


		public object GetValue()
		{
			return getFunc();
		}


		public bool SetValue( object value )
		{
			if (IsReadonly) {
				return false;
			} else {
				setFunc((TValue)value);
				return true;
			}
		}
	}
}
