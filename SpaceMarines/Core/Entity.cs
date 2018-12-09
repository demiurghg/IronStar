using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;

namespace SpaceMarines.Core {
	public class Entity {

		/// <summary>
		/// Entity's ID
		/// </summary>
		public readonly uint ID;

		/// <summary>
		/// Position
		/// </summary>
		public Vector2 Position {
			get; set;
		}

		/// <summary>
		/// Primary angle
		/// </summary>
		public float Angle {
			get; set;
		}

		/// <summary>
		/// Secondary angle
		/// </summary>
		public float HeadAngle {
			get; set;
		}

		/// <summary>
		/// Forward direction
		/// </summary>
		public Vector2 Forward {
			get {
				float rads	= MathUtil.DegreesToRadians( Angle );
				float cos	= (float)Math.Cos( rads );
				float sin	= (float)Math.Cos( rads );
				return new Vector2( cos, sin );
			}
		}
		

		/// <summary>
		/// Entity's sprite model
		/// </summary>
		public string Model {
			get; set;
		}


		/// <summary>
		/// Entity's sprite model
		/// </summary>
		public string HeadModel {
			get; set;
		}


		/// <summary>
		/// Entity's SFX
		/// </summary>
		public string Sfx {
			get; set;
		}




		/// <summary>
		/// Creates instance of Entity
		/// </summary>
		/// <param name="world"></param>
		/// <param name="factory"></param>
		public Entity ( uint id, GameWorld world, EntityFactory factory )
		{
			this.ID	=	id;
		}


		/// <summary>
		/// Updates entity's state
		/// </summary>
		/// <param name="gameTime"></param>
		public virtual void Update ( GameTime gameTime )
		{
		}


		/// <summary>
		/// Inflicks damage to the entity
		/// </summary>
		/// <param name="amount"></param>
		public virtual void Damage ( int amount )
		{
		}




	}
}
