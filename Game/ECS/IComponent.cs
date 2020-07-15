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
		/// Called when component has been added to entity
		/// </summary>
		/// <param name="entityId">Entity that component has been added to</param>
		void Added( uint entityId );

		/// <summary>
		/// Called when component has been removed from entity
		/// </summary>
		/// <param name="entityId">Entity that component has been removed from</param>
		void Removed( uint entityId );

		/// <summary>
		/// Saves all component data to stream
		/// </summary>
		/// <param name="stream"></param>
		void Save( Stream stream );

		/// <summary>
		/// Loads component data from stream
		/// </summary>
		/// <param name="stream"></param>
		void Load( Stream stream );
	}
}
