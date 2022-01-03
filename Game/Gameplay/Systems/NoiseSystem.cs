using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Configuration;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.Gameplay.Components;

namespace IronStar.Gameplay.Systems
{
	[ConfigClass]
	class NoiseSystem : StatelessSystem<Transform,NoiseComponent>
	{
		[Config] public static bool  ShowNoise { get; set; }
		[Config] public static float DecayRate { get; set; } = 0.33f;
		[Config] public static float Threshold { get; set; } = 0.1f;
		

		protected override void Process( Entity entity, GameTime gameTime, Transform transform, NoiseComponent noise )
		{
			float decay = 	(float)Math.Pow( DecayRate, gameTime.ElapsedSec );

			if (noise.Level>Threshold)
			{
				noise.Level *= decay;
			}
			else
			{
				noise.Level = 0;
			}

			if (ShowNoise)
			{
				var dr = entity.gs.Game.RenderSystem.RenderWorld.Debug.Async;

				if (noise.Level>Threshold)
				{
					var t = Matrix.Translation(transform.Position);
					dr.DrawRing( t, noise.Level, Color.Red, 32, 8, 1 );
				}
			}
		}
	}
}
