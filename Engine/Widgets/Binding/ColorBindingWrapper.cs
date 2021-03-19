using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;

namespace Fusion.Widgets.Binding
{
	public class ColorBindingWrapper
	{
		readonly IValueBinding binding = null;
		Color fallbackValue;

		public IValueBinding Red { get; private set; }
		public IValueBinding Green { get; private set; }
		public IValueBinding Blue { get; private set; }
		public IValueBinding Alpha { get; private set; }
		public IValueBinding Hue { get; private set; }
		public IValueBinding Sat { get; private set; }
		public IValueBinding Value { get; private set; }

		public ColorBindingWrapper( IValueBinding binding ) : this(binding, Color.White) {}

		
		public ColorBindingWrapper( IValueBinding binding, Color initialValue )
		{
			fallbackValue	=	initialValue;

			if (binding.ValueType!=typeof(Color))
			{
				throw new ValueBindingException("ColorBindingWrapper do not support value type " + binding.ValueType.ToString() );
			}

			this.binding	=	binding;

			Red		=	new ColorComponentBinding( this, ColorComponent.Red		);
			Green	=	new ColorComponentBinding( this, ColorComponent.Green	);
			Blue	=	new ColorComponentBinding( this, ColorComponent.Blue	);
			Alpha	=	new ColorComponentBinding( this, ColorComponent.Alpha	);
			Hue		=	new ColorComponentBinding( this, ColorComponent.Hue		);
			Sat		=	new ColorComponentBinding( this, ColorComponent.Sat		);
			Value	=	new ColorComponentBinding( this, ColorComponent.Value	);
		}

		


		public bool SetRGBColor( Color colorValue, ValueSetMode mode )
		{
			fallbackValue	=	colorValue;

			if (binding!=null)
			{
				return binding.SetValue( colorValue, mode );
			}

			return true;
		}


		public Color GetRGBValue()
		{
			if (binding!=null)
			{
				return (Color)binding.GetValue();
			}
			else
			{
				return fallbackValue;
			}
		}

		
		public HSVColor GetHSVColor()
		{
			return HSVColor.ConvertRgbToHsv( GetRGBValue() );
		}


		public bool SetHSVColor(HSVColor hsv, ValueSetMode mode)
		{
			return SetRGBColor( new Color( HSVColor.ConvertHsvToRgb(hsv) ), mode );
		}
	}
}
