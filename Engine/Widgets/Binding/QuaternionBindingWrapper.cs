using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;

namespace Fusion.Widgets.Binding
{
	public class QuaternionBindingWrapper
	{
		readonly IValueBinding binding = null;
		Quaternion fallbackValue;

		public QuaternionBindingWrapper( IValueBinding binding ) : this(binding, Quaternion.Identity) {}

		
		public QuaternionBindingWrapper( IValueBinding binding, Quaternion initialValue )
		{
			fallbackValue	=	initialValue;

			if (binding.ValueType!=typeof(Quaternion))
			{
				throw new ValueBindingException("QuaternionBindingWrapper do not support value type " + binding.ValueType.ToString() );
			}

			this.binding	=	binding;
		}

		


		public bool SetQuaternion( Quaternion colorValue, ValueSetMode mode )
		{
			fallbackValue	=	colorValue;

			if (binding!=null)
			{
				return binding.SetValue( colorValue, mode );
			}

			return true;
		}


		public Quaternion GetQuaternion()
		{
			if (binding!=null)
			{
				return (Quaternion)binding.GetValue();
			}
			else
			{
				return fallbackValue;
			}
		}
	}
}
