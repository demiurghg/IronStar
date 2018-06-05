using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core;

namespace IronStar.Core {
	public interface IEntity {

		/// <summary>
		/// Writes entity state to stream 
		/// </summary>
		/// <param name="witer"></param>
		void Write ( BinaryWriter witer );

		/// <summary>
		/// Reads entity state from stream
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="lerpFactor"></param>
		void Read ( BinaryReader reader, float lerpFactor );

		/// <summary>
		/// Updates entity internal state.
		/// </summary>
		/// <param name="gameTime"></param>
		void Update ( GameTime gameTime );

		/// <summary>
		/// Updates entity presentation
		/// </summary>
		/// <param name="gameTime"></param>
		void Draw ( GameTime gameTime );

		/// <summary>
		/// Indicates whether entity 
		/// should be removed from world.
		/// </summary>
		bool IsDead { get; }

	}
}
