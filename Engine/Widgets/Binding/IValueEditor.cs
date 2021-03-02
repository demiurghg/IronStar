using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Frames;

namespace Fusion.Widgets.Binding 
{
	public interface IValueEditor
	{
		/// <summary>
		/// Creates editor control
		/// </summary>
		/// <param name="binding"></param>
		/// <returns></returns>
		Frame CreateEditor( IValueBinding binding );
	}
}
