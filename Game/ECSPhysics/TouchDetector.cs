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
	public class TouchDetector : Component, IEnumerable<Entity>
	{
		public readonly List<Entity> touches;

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

		private TouchDetector( IEnumerable<Entity> touches )
		{
			this.touches = new List<Entity>( touches );
		}

		public TouchDetector()
		{
			touches = new List<Entity>();
		}

		public override IComponent Clone()
		{
			return new TouchDetector( touches );
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
