using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Widgets.Binding 
{
	public interface IValueBinding 
	{
		/// <summary>
		/// Gets value.
		/// </summary>
		/// <returns>Value</returns>
		object GetValue ();

		/// <summary>
		/// Initiate value changes.
		/// Proper interface implementation should store old value here.
		/// </summary>
		void Initiate();

		/// <summary>
		/// Commits value changes for interactive controls like sliders or color-pickers
		/// </summary>
		void Commit();

		/// <summary>
		/// Cancels changes.
		/// Proper interface implementation should restore old value.
		/// </summary>
		void Cancel();

		/// <summary>
		/// Sets value. Returns TRUE if scceeded, FALSE otherwice.
		/// </summary>
		/// <param name="value">Value to set</param>
		/// <returns></returns>
		bool SetValue (object value);

		/// <summary>
		/// Indicates that given binding is read-only.
		/// </summary>
		bool IsReadonly { get; }

		/// <summary>
		/// Gets value type
		/// </summary>
		Type ValueType { get; }
	}
}
