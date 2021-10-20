using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Gameplay.Components;
using IronStar.SFX;

namespace IronStar.Gameplay.Systems
{
	public class InventorySystem : StatelessSystem<InventoryComponent>
	{
		public override void Add( IGameState gs, Entity e )
		{
			base.Add( gs, e );
		}

		public override void Remove( IGameState gs, Entity e )
		{
			base.Remove( gs, e );

			var inventory = e.GetComponent<InventoryComponent>();

			foreach ( var item in inventory )
			{
				item.Kill();
			}
		}

		protected override void Process( Entity entity, GameTime gameTime, InventoryComponent component1 )
		{
			throw new NotImplementedException();
		}
	}
}
