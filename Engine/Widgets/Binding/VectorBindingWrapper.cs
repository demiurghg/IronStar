using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;

namespace Fusion.Widgets.Binding
{
	public class VectorBindingWrapper
	{
		readonly IValueBinding binding = null;
		Vector3 fallbackValue;

		public IValueBinding X { get; private set; }
		public IValueBinding Y { get; private set; }
		public IValueBinding Z { get; private set; }

		public VectorBindingWrapper( IValueBinding binding ) : this(binding, Vector3.Zero) {}

		
		public VectorBindingWrapper( IValueBinding binding, Vector3 initialValue )
		{
			fallbackValue	=	initialValue;

			if (binding.ValueType!=typeof(Vector3))
			{
				throw new ValueBindingException("VectorBindingWrapper do not support value type " + binding.ValueType.ToString() );
			}

			this.binding	=	binding;

			X	=	new VectorComponentBinding( this, 0 );
			Y	=	new VectorComponentBinding( this, 1 );
			Z	=	new VectorComponentBinding( this, 2 );
		}

		


		public bool SetVector( Vector3 colorValue, ValueSetMode mode )
		{
			fallbackValue	=	colorValue;

			if (binding!=null)
			{
				return binding.SetValue( colorValue, mode );
			}

			return true;
		}


		public Vector3 GetVector()
		{
			if (binding!=null)
			{
				return (Vector3)binding.GetValue();
			}
			else
			{
				return fallbackValue;
			}
		}
	}
}
