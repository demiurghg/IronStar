using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Graphics;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using IronStar.Editor.Manipulators;
using Fusion;
using IronStar.Mapping;

namespace IronStar.Editor.Manipulators 
{
	public class NullTool : ITool 
	{
		public NullTool ()
		{
		}

		public void Update ( GameTime gameTime, int x, int y )
		{
		}

		public bool IsManipulating 
		{
			get { return false; }
		}

		public string ManipulationText 
		{
			get { return ""; }
		}

		public bool StartManipulation ( int x, int y, bool useSnapping )
		{
			return false;
		}

		public void UpdateManipulation ( int x, int y ) {}
		public void StopManipulation ( int x, int y ) {}
	}
}
