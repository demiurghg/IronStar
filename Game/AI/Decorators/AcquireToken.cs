using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using IronStar.BTCore;
using IronStar.ECS;
using IronStar.ECSFactories;

namespace IronStar.AI.Actions
{
	public class AcquireToken : Decorator
	{
		readonly AITokenPool pool;
		AIToken token;

		public AcquireToken( AITokenPool pool, BTNode node ) : base(node)
		{
			this.pool	=	pool;
		}

		public override bool Initialize( Entity entity )
		{
			token = pool.Acquire(entity);
			return token!=null;
		}

		public override BTStatus Update( GameTime gameTime, Entity entity, bool cancel )
		{
			return Node.Tick(gameTime, entity, cancel);
		}

		public override void Terminate( Entity entity, BTStatus status )
		{
			if (token!=null)
			{
				token.Release();
				token = null;
			}
		}
	}
}
