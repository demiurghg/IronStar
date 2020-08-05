using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.ECSPhysics
{
	public class TouchDetector : IComponent, IEnumerable<Entity>
	{
		public readonly List<Entity> touches = new List<Entity>();

		public void AddTouch( Entity touch )
		{
			if (touch!=null)
			{
				touches.Add( touch );
			}
		}

		public void ClearTouches()
		{
			touches.Clear();
		}

		public void Load( GameState gs, Stream stream )
		{
		}

		public void Save( GameState gs, Stream stream )
		{
		}

		public IEnumerator<Entity> GetEnumerator()
		{
			return ( (IEnumerable<Entity>)touches ).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (IEnumerable<Entity>)touches ).GetEnumerator();
		}
	}
}
