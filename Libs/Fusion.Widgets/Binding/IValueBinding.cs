using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Widgets.Binding {
	public interface IValueBinding<TValue> {
		TValue GetValue ();
		bool SetValue (TValue value);
		bool IsReadonly {
			get;
		}
	}
}
