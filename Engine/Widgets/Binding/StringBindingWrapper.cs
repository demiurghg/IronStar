using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;

namespace Fusion.Widgets.Binding
{
	public class StringBindingWrapper
	{
		readonly IValueBinding binding;
		string fallbackValue;

		public StringBindingWrapper( IValueBinding binding, string initialValue )
		{
			fallbackValue	=	initialValue;
			this.binding	=	binding;
		}

		
		public StringBindingWrapper( IValueBinding binding )
		{
			this.binding=binding;
		}


		public void Initiate()
		{
			binding?.Initiate();
		}


		public void Commit()
		{
			binding?.Commit();
		}

		
		public void Cancel()
		{
			binding?.Cancel();
		}

		
		public bool SetValue( string stringValue )
		{
			fallbackValue	=	stringValue;

			if (binding!=null)
			{
				object value;
				if ( StringConverter.TryConvertFromString( binding.ValueType, stringValue, out value ) )
				{
					return binding.SetValue( value );
				}
				else
				{
					return false;
				}
			}

			return true;
		}


		public string GetValue()
		{
			if (binding!=null)
			{
				var value = binding.GetValue();

				return StringConverter.ConvertToString( value );
			}
			else
			{
				return fallbackValue;
			}
		}
	}
}
