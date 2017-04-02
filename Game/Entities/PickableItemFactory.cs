using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using IronStar.Items;
//using BEPUphysics.


namespace IronStar.Entities {

	public class PickableItemFactory : EntityFactory {

		public string ItemName { get; set; } = "";
		
		public ItemFactory factory;

		
		public override EntityController Spawn( Entity entity, GameWorld world )
		{
			if (string.IsNullOrWhiteSpace(ItemName)) {
				Log.Warning("ProxyFactory: itemname is null or white space, null-entity spawned");
				return null;
			}

			factory = world.Content.Load<ItemFactory>(@"items\" + ItemName);

			return new PickableItem( entity, world, factory );
		}


		public override void Draw( DebugRender dr, Matrix transform, Color color )
		{
			if (factory==null) {
				base.Draw( dr, transform, color );
			} else {
				factory.Draw( dr, transform, color );
			}
		}
	}

}
