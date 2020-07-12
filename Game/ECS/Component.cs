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
		/// </summary>
		public readonly uint OwnerId; 

		/// <summary>
		/// Component constructor
		/// </summary>
		/// <param name="ownerId"></param>
		public Component ( uint ownerId )
		{
			this.OwnerId	=	ownerId;
		}


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
