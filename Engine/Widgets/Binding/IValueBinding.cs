using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Widgets.Binding 
{
	public enum ValueSetMode
	{
		Default,
		InteractiveInitiate,
		InteractiveUpdate,
		InteractiveComplete,
	}

	public interface IValueBinding 
	{
		/// <summary>
		/// Gets value.
		/// </summary>
		/// <returns>Value</returns>
		object GetValue ();

		/// <summary>
		/// Sets value. Returns TRUE if scceeded, FALSE otherwice.
		/// </summary>
		/// <param name="value">Value to set</param>
		/// <returns></returns>
		bool SetValue (object value, ValueSetMode setMode);

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
