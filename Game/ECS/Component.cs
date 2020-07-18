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
		private Entity entity = null;
		public Entity Entity { get { return entity; } }

		public virtual void Added( GameState gs, Entity entity )
		{
			if (this.entity!=null || entity==null)
			{
				throw new InvalidOperationException("Component.Added : inconsistent entity and component");
			}

			this.entity = entity;
		}

		public virtual void Removed( GameState gs )
		{
			entity = null;
		}

		public virtual void Load( GameState gs, Stream stream )
		{
		}

		public virtual void Save( GameState gs, Stream stream )
		{
		}
	}
}
