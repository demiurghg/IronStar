using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;

namespace IronStar.AI.BehaviorTree
{
	public abstract class BTNode
	{
		BTStatus status = BTStatus.Undefined;

		internal BTStatus Tick (GameTime gameTime, Entity entity)
		{
			if (status!=BTStatus.InProgress) 
			{
				Initialize();
			}

			status = Update( gameTime, entity );

			if (status!=BTStatus.InProgress) 
			{
				Terminate(status);
			}

			return status;
		}

		public virtual  void Initialize() {}
		public virtual  void Terminate(BTStatus status) {}
		public abstract BTStatus Update(GameTime gameTime, Entity entity);
		public abstract void Attach( BTNode node );
		public virtual	void Cancel() {}
	}
}
