using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;

namespace Fusion.Widgets.Binding
{
	enum ColorComponent
	{
		Red,
		Green,
		Blue,
		Alpha,

		Hue,
		Sat,
		Value
	}

	class ColorComponentBinding : IValueBinding
	{
		readonly ColorBindingWrapper binding;
		readonly ColorComponent component;

		public ColorComponentBinding( ColorBindingWrapper binding, ColorComponent component )
		{
			this.binding	=	binding;
			this.component	=	component;
		}

		public bool IsReadonly { get { return false; } }
		public Type ValueType { get { return typeof(float); } }

		public object GetValue()
		{
			switch (component)
			{
				case ColorComponent.Red		:	return binding.GetRGBValue().R;
				case ColorComponent.Green	:	return binding.GetRGBValue().G;
				case ColorComponent.Blue	:	return binding.GetRGBValue().B;
				case ColorComponent.Alpha	:	return binding.GetRGBValue().A;
				case ColorComponent.Hue		:	return binding.GetHSVColor().H;
				case ColorComponent.Sat		:	return binding.GetHSVColor().S;
				case ColorComponent.Value	:	return binding.GetHSVColor().V;
			}
			return 0;
		}

		public bool SetValue( object value, ValueSetMode setMode )
		{
			var rgb = binding.GetRGBValue();
			var hsv = binding.GetHSVColor();

			var	floatValue	=	(float)value;
			var byteValue	=	(byte)MathUtil.Clamp(floatValue, 0, 255 );

			switch (component)
			{
				case ColorComponent.Red:	rgb.R	=	byteValue;	binding.SetRGBColor(rgb, setMode);	break;
				case ColorComponent.Green:	rgb.G	=	byteValue;	binding.SetRGBColor(rgb, setMode);	break;
				case ColorComponent.Blue:	rgb.B	=	byteValue;	binding.SetRGBColor(rgb, setMode);	break;
				case ColorComponent.Alpha:	rgb.A	=	byteValue;	binding.SetRGBColor(rgb, setMode);	break;
				case ColorComponent.Hue:	hsv.H	=	floatValue;	binding.SetHSVColor(hsv, setMode);	break;
				case ColorComponent.Sat:	hsv.S	=	floatValue;	binding.SetHSVColor(hsv, setMode);	break;
				case ColorComponent.Value:	hsv.V	=	floatValue;	binding.SetHSVColor(hsv, setMode);	break;
			}

			return true;
		}
	}
}
