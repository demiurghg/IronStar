using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	public interface IComponent
	{
		/// <summary>
		/// Saves all component data to stream
		/// </summary>
		/// <param name="stream"></param>
		void Save( GameState gs, Stream stream );

		/// <summary>
		/// Loads component data from stream
		/// </summary>
		/// <param name="stream"></param>
		void Load( GameState gs, Stream stream );
	}
}
