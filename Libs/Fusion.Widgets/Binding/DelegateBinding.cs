using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Widgets.Binding {
	public class DelegateBinding<TValue> : IValueBinding<TValue> {

		readonly Func<TValue> getFunc;
		readonly Action<TValue> setFunc;

		
		public DelegateBinding ( Func<TValue> getFunc, Action<TValue> setFunc )
		{
			this.getFunc	=	getFunc;
			this.setFunc	=	setFunc;
		}


		public bool IsReadonly {
			get {
				return setFunc==null;
			}
		}


		public TValue GetValue()
		{
			return getFunc();
		}


		public bool SetValue( TValue value )
		{
			if (IsReadonly) {
				return false;
			} else {
				setFunc(value);
				return true;
			}
		}
	}
}
