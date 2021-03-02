using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Frames;
using Fusion.Widgets.Binding;

namespace Fusion.Widgets.Advanced
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple=true)]
	public abstract class AEEditorAttribute : Attribute 
	{
		public abstract Frame CreateEditor( AEPropertyGrid grid, string name, IValueBinding binding );
	}
}
