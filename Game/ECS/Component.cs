using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	public class Component
	{
		/// <summary>
		/// Owner entity ID
		/// Components are able to migrate from entity to entity
		/// </summary>
		public uint OwnerId = 0; 


		/// <summary>
		/// Saves all component data to stream
		/// </summary>
		/// <param name="stream"></param>
		public virtual void Save( Stream stream )
		{
		}


		/// <summary>
		/// Loads component data from stream
		/// </summary>
		/// <param name="stream"></param>
		public virtual void Load( Stream stream )
		{
		}
	}
}
