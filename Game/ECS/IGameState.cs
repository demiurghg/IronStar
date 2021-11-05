using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	public interface IGameState	: IDisposable
	{
		bool Paused { get; set; }

		Game Game { get; }

		ContentManager Content { get; }

		Entity Spawn(IFactory factory);

		void ForceRefresh();

		void KillAll();

		void Update ( GameTime gameTime );

		Entity GetEntity(uint id);

		TService GetService<TService>() where TService : class;

		IEnumerable<ISystem> Systems { get; }

		IEnumerable<Entity> QueryEntities( Aspect aspect );
	}
}
