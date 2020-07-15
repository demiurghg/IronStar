using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	public class Component : IComponent
	{
		private uint entityId = 0;
		public uint EntityID { get { return entityId; } }

		public void Added( uint entityId )
		{
			if (this.entityId!=0 || entityId==0)
			{
				throw new InvalidOperationException("Component.Added : bad entity ID");
			}

			this.entityId = entityId;
		}

		public void Removed( uint entityId )
		{
			if (this.entityId!=entityId)
			{
				throw new InvalidOperationException("Component.Removed : bad entity ID");
			}
			this.entityId = 0;
		}

		public void Load( Stream stream )
		{
		}

		public void Save( Stream stream )
		{
		}
	}
}
