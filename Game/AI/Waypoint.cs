using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.AI {

	public class Waypoint {

		public readonly Vector3 Position;
		public readonly Vector3 Normal;
		public bool Walkable;
		public bool Impassible;

		public bool Discovered;
		public int BfsDepth;

		public float Importance;

		public Waypoint[] Links;

		public Vector2 ProjectedPoint {
			get { return new Vector2( Position.X, Position.Z ); }
		}

		public float ProjectedHeight {
			get { return Position.Y; }
		}


		/// <summary>
		/// Creates instance of the waypoint
		/// </summary>
		/// <param name="position"></param>
		public Waypoint ( Vector3 position, Vector3 normal, bool walkable )
		{
			this.Position	=	position;
			this.Normal		=	normal;
			this.Walkable	=	walkable;
		}

	}
}
