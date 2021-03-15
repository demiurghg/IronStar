using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;

namespace IronStar.Editor.Manipulators
{
	public interface ITool
	{
		bool StartManipulation ( int x, int y, bool snap );
		void UpdateManipulation ( int x, int y );
		void StopManipulation ( int x, int y );
		void Update ( GameTime gameTime, int x, int y );
		string ManipulationText { get; }
		bool IsManipulating { get; }
	}
}
