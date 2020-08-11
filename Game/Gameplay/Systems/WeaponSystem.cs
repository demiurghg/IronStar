using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;
using IronStar.Gameplay.Components;

namespace IronStar.Gameplay.Systems
{
	class WeaponSystem : ISystem
	{
		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }


		Aspect weaponAspect			=	new Aspect().Include<WeaponComponent>();
		Aspect armedEntitiesAspect	=	new Aspect().Include<InventoryComponent>();

		
		public void Update( GameState gs, GameTime gameTime )
		{
			var armedEntities = gs.QueryEntities( armedEntitiesAspect );

			foreach ( var armedEntity in armedEntities )
			{
				 //var inventory
			}
		}
	}
}
