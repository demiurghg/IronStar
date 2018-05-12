using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Graphics;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion;
using IronStar.Mapping;

namespace IronStar.Editor2.Manipulators {
	public class ArrowHandle : Handle {

		/// <summary>
		/// Constrcutor
		/// </summary>
		public ArrowHandle ( MapEditor editor, Color color1, Color color2 ) : base(editor)
		{
		}


		public abstract bool StartHandling ( int x, int y );
		public abstract void UpdateHandling ( int x, int y );
		public abstract void StopHandling ( int x, int y );
		public abstract void DrawHandle ( DebugRender dr, Ray pickRay, bool active );
	}
}
