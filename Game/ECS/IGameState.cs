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
		Game Game { get; }

		ContentManager Content { get; }

		Entity Spawn();

		Entity Spawn(string classname);

		void KillAll();

		void Teleport( Entity e, Vector3 position, Quaternion rotation );

		void Update ( GameTime gameTime );

		void AddSystem( ISystem system );

		TService GetService<TService>() where TService : class;
	}
}
