using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;

namespace IronStar.BTCore
{
	public abstract class BTNode
	{
		BTStatus status = BTStatus.Undefined;

		internal BTStatus Tick (GameTime gameTime, Entity entity)
		{
			if (status!=BTStatus.InProgress) 
			{
				if (!Initialize(entity))
				{
					return BTStatus.Failure;
				}
			}

			status = Update( gameTime, entity );

			if (status!=BTStatus.InProgress) 
			{
				Terminate(entity, status);
			}

			return status;
		}

		public virtual  bool Initialize(Entity entity) { return true; }
		public virtual  void Terminate(Entity entity, BTStatus status) {}
		public abstract BTStatus Update(GameTime gameTime, Entity entity);
		public abstract void Attach( BTNode node );
		public virtual	void Cancel() {}
	}
}
