using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	public sealed class Entity
	{
		/// <summary>
		/// Entity updated once on gamestate startup.
		/// </summary>
		public const int	Static	=	0;	


		/// <summary>
		/// Unique entity ID
		/// </summary>
		public readonly uint Id;

		/// <summary>
		/// Entity position :
		/// </summary>
		public Vector3	Position;

		/// <summary>
		/// Entity rotation
		/// </summary>
		public Quaternion Rotation;

		/// <summary>
		/// Bit mask indicating which system process given entity
		/// </summary>
		internal BitSet Mapping;

		/// <summary>
		/// Bit mask indicating general entity states, like static, save/load, network, etc.
		/// </summary>
		public BitSet EntityState;

		/// <summary>
		/// Entity constructor
		/// </summary>
		/// <param name="id"></param>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		public Entity ( uint id, Vector3 position, Quaternion rotation )
		{
			this.Id			=	id;
			this.Mapping	=	new BitSet(0);
			this.Position	=	position;
			this.Rotation	=	rotation;
		}

		/// <summary>
		/// Entity constructor
		/// </summary>
		/// <param name="id"></param>
		public Entity ( uint id, Vector3 position ) : this( id, position, Quaternion.Identity )
		{
		}

		/// <summary>
		/// Entity constructor
		/// </summary>
		/// <param name="id"></param>
		public Entity ( uint id ) : this( id, Vector3.Zero, Quaternion.Identity )
		{
		}
	}
}
