using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;

namespace Fusion.Widgets.Binding
{
	public class NumericBindingWrapper
	{
		readonly IValueBinding binding;
		double fallbackValue;

		public NumericBindingWrapper( IValueBinding binding, double initialValue )
		{
			fallbackValue	=	initialValue;

			if (binding.ValueType.IsNumericType())
			{
				this.binding	=	binding;
			}
		}

		public NumericBindingWrapper( IValueBinding binding )
		{
			this.binding=binding;
		}

		public bool SetValue( double numericValue )
		{
			fallbackValue	=	numericValue;

			if (binding!=null)
			{
				binding.SetValue( Convert.ChangeType( numericValue, binding.ValueType ) );
			}

			return true;
		}


		public double GetValue()
		{
			if (binding!=null)
			{
				return (double)Convert.ChangeType( binding.GetValue(), typeof(double) );
			}
			else
			{
				return fallbackValue;
			}
		}
	}
}
