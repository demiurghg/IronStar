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

		//Entity Spawn();
		Entity Spawn(IEntityFactory factory);

		[Obsolete]
		Entity Spawn(string classname);

		[Obsolete]
		Entity Spawn(string classname, Vector3 position, Quaternion rotation);

		//[Obsolete]
		//Entity Spawn(params IComponent[] components);

		void KillAll();

		[Obsolete]
		void Teleport( Entity e, Vector3 position, Quaternion rotation );

		void Update ( GameTime gameTime );

		//void AddSystem( ISystem system );

		Entity GetEntity(uint id);

		TService GetService<TService>() where TService : class;

		IEnumerable<Entity> QueryEntities( Aspect aspect );
	}


	public static class IGameStateExtensions
	{
		class CompoundFactory : IEntityFactory
		{
			readonly IComponent[] components;
			public CompoundFactory( IComponent[] components )
			{
				this.components	=	components;
			}

			public void Construct( Entity entity, IGameState gs )
			{
				foreach ( var c in components )
				{
					entity.AddComponent( c );
				}
			}
		}

		public static Entity Spawn( this IGameState gs, params IComponent[] components )
		{
			return gs.Spawn( new CompoundFactory(components) );
		}
	}
}
