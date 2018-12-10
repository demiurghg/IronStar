using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Binding {

	public interface IValueBinding {

		/// <summary>
		/// Gets value.
		/// </summary>
		/// <returns></returns>
		object GetValue ();

		/// <summary>
		/// Sets value. Returns TRUE if scceeded, FALSE otherwice.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		bool SetValue (object value);

		/// <summary>
		/// Indicates that given binding is read-only.
		/// </summary>
		bool IsReadonly {
			get;
		}
	}
}
