using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;
using IronStar.Gameplay.Components;
using IronStar.SFX;

namespace IronStar.Gameplay.Systems
{
	class ExplosionSystem : StatelessSystem<ExplosiveComponent, HealthComponent>
	{
		protected override void Process( Entity entity, GameTime gameTime, ExplosiveComponent explosive, HealthComponent health )
		{
			if (!explosive.Initiated && health.Health < 0)
			{
				explosive.Initiated = true;
				entity.AddComponent( new FXComponent(explosive.BurningFX, true) );
				entity.AddComponent( 
					new ProjectileComponent(entity, explosive.Radius, explosive.Timeout, explosive.ExplosionFX, explosive.Damage, explosive.Impulse) 
					{
						Options = ProjectileOptions.TimeoutDetonation,
					}
				);
			}
		}
	}
}
