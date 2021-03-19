using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;

namespace Fusion.Widgets.Binding
{
	public class VectorComponentBinding : IValueBinding
	{
		readonly VectorBindingWrapper binding;
		readonly int component;

		public VectorComponentBinding( VectorBindingWrapper binding, int component )
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
				case 0 :	return binding.GetVector().X;
				case 1 :	return binding.GetVector().Y;
				case 2 :	return binding.GetVector().Z;
			}
			return 0;
		}

		public bool SetValue( object value, ValueSetMode setMode )
		{
			var xyz		=	binding.GetVector();
			var	fval	=	(float)value;

			switch (component)
			{
				case 0: xyz.X	=	fval;	binding.SetVector(xyz, setMode);	break;
				case 1: xyz.Y	=	fval;	binding.SetVector(xyz, setMode);	break;
				case 2: xyz.Z	=	fval;	binding.SetVector(xyz, setMode);	break;
			}

			return true;
		}
	}
}
